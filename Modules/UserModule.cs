using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using DotNetOpenAuth;
using DotNetOpenAuth.ApplicationBlock;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth;
using DotNetOpenAuth.OAuth.ChannelElements;
using DotNetOpenAuth.OAuth.Messages;
using log4net;
using Nancy;

namespace CollapsedToto
{
    using BaseProcessUserAuthorizationFunc = Func<ConsumerBase, string, string, AuthorizedTokenResponse>;
    using ProcessIncomingMessageFunc = Action<Channel, IProtocolMessage>;
    using ReceiveFunc = Func<Channel, Dictionary<string, string>, MessageReceivingEndpoint, IProtocolMessage>;
    using RequestTokenFunc = Func<UserAuthorizationResponse, string>;
    static class DotNetOpenAuthExt
    {
        static ILog logger = null;
        static ILog Logger
        {
            get 
            { 
                return logger ?? (logger = LogManager.GetLogger(typeof(DotNetOpenAuthExt)));
            } 
        }
            
        static ReceiveFunc receive = null;
        static ReceiveFunc Receive
        {
            get
            {
                if (receive == null)
                {
                    var method = typeof(Channel).GetMethod("Receive", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    receive = (ReceiveFunc)method.CreateDelegate(typeof(ReceiveFunc));
                }

                return receive;
            }
        }

        static ProcessIncomingMessageFunc processIncomingMessage = null;
        static ProcessIncomingMessageFunc ProcessIncomingMessage
        {
            get
            {
                if (processIncomingMessage == null)
                {
                    var method = typeof(Channel).GetMethod("ProcessIncomingMessage", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    processIncomingMessage = (ProcessIncomingMessageFunc)method.CreateDelegate(typeof(ProcessIncomingMessageFunc));
                }

                return processIncomingMessage;
            }
        }
        public static bool TryReadFromRequest<TRequest>(this Channel chan, Request httpRequest, out TRequest request)
            where TRequest : class, IProtocolMessage
        {
            if (httpRequest == null)
            {
                throw new ArgumentNullException("httpRequest");
            }
            Contract.Ensures(Contract.Result<bool>() == (Contract.ValueAtReturn<TRequest>(out request) != null));

            IProtocolMessage untypedRequest = null;
            if (Logger.IsInfoEnabled && httpRequest.Url != null)
            {
                Logger.InfoFormat("Scanning incoming request for messages: {0}", httpRequest.Url.ToString());
            }
            IDirectedProtocolMessage requestMessage = null;

            Contract.Assume(httpRequest.Form != null && httpRequest.Query != null);
            var fields = httpRequest.Form.ToDictionary();
            if (fields.Count == 0 && httpRequest.Method != "POST") { // OpenID 2.0 section 4.1.2
                fields = ((IDictionary<string, object>)(DynamicDictionary)httpRequest.Query).ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
            }

            MessageReceivingEndpoint recipient = null;
            try {
                HttpDeliveryMethods method;
                Enum.TryParse<HttpDeliveryMethods>(httpRequest.Method + "Request", true, out method);
                recipient = new MessageReceivingEndpoint(httpRequest.Url.SiteBase, method);
            } catch (ArgumentException ex) {
                
                Logger.WarnFormat("Unrecognized HTTP request: {0}", ex);
                requestMessage = null;
            }

            if (recipient != null)
            {
                requestMessage = (IDirectedProtocolMessage)Receive(chan, fields, recipient);
            }

            if (requestMessage != null) {
                Logger.DebugFormat("Incoming request received: {0}", requestMessage.GetType().Name);

                var directRequest = requestMessage as IHttpDirectRequest;
                if (directRequest != null) {
                    foreach (string header in httpRequest.Headers.Keys) {
                        directRequest.Headers[header] = httpRequest.Headers[header].ToString();
                    }
                }

                ProcessIncomingMessage(chan, requestMessage);
                untypedRequest = requestMessage;
            }

            if (untypedRequest == null) {
                request = null;
                return false;
            }

            request = untypedRequest as TRequest;

            return true;
        }

        static RequestTokenFunc requestToken = null;
        static RequestTokenFunc RequestToken
        {
            get
            {
                if (requestToken == null)
                {
                    var property = typeof(UserAuthorizationResponse).GetProperty("RequestToken", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    var method = property.GetGetMethod(true);
                    requestToken = (RequestTokenFunc)method.CreateDelegate(typeof(RequestTokenFunc));
                }

                return requestToken;
            }
        }

        static BaseProcessUserAuthorizationFunc baseProcessUserAuthorization = null;
        static BaseProcessUserAuthorizationFunc BaseProcessUserAuthorization
        {
            get
            {
                if (baseProcessUserAuthorization == null)
                {
                    var method = typeof(ConsumerBase).GetMethod("ProcessUserAuthorization", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    baseProcessUserAuthorization = (BaseProcessUserAuthorizationFunc)
                        method.CreateDelegate(typeof(BaseProcessUserAuthorizationFunc));
                }

                return baseProcessUserAuthorization;
            }
        }

        public static AuthorizedTokenResponse ProcessUserAuthorization(this WebConsumer cons, Request request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            UserAuthorizationResponse authorizationMessage;
            if (cons.Channel.TryReadFromRequest<UserAuthorizationResponse>(request, out authorizationMessage))
            {
                string requestToken = RequestToken(authorizationMessage);
                string verifier = authorizationMessage.VerificationCode;
                return BaseProcessUserAuthorization(cons, requestToken, verifier);
            }
            else
            {
                return null;
            }
        }

    }

    [Prefix("/user")]
    public class UserModule : BaseModule
    {
        private ILog logger = null;
        public UserModule()
        {
            logger = LogManager.GetLogger(typeof(UserModule));
        }

        public static readonly ServiceProviderDescription ServiceDescription = new ServiceProviderDescription {
            RequestTokenEndpoint = new MessageReceivingEndpoint("https://api.twitter.com/oauth/request_token", HttpDeliveryMethods.PostRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
            UserAuthorizationEndpoint = new MessageReceivingEndpoint("https://api.twitter.com/oauth/authorize", HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
            AccessTokenEndpoint = new MessageReceivingEndpoint("https://api.twitter.com/oauth/access_token", HttpDeliveryMethods.PostRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
            TamperProtectionElements = new ITamperProtectionChannelBindingElement[] { new HmacSha1SigningBindingElement() },
        };

        private InMemoryTokenManager TokenManager {
            get {
                if (Session["tokenManager"] == null) {
                    Session["tokenManager"] = new InMemoryTokenManager(Constants.TwitterConsumerKey, Constants.TwitterConsumerSecret);
                }

                return (InMemoryTokenManager)Session["tokenManager"];
            }
        }

        private WebConsumer TwitterSignIn {
            get {
                if (Session["signInConsumer"] == null) {
                    Session["signInConsumer"] = new WebConsumer(ServiceDescription, TokenManager);
                }

                return (WebConsumer)Session["signInConsumer"];
            }
        }

        [Get("/signin")]
        public dynamic signin(dynamic param)
        {
            logger.Debug("Try SignIn");
            var callbackUriString = Request.Url.SiteBase + "/user/callback";
            if (param.redirect != null)
            {
                callbackUriString += string.Format("?redirect={0}", param.redirect);
            }
            var callbackUri = new Uri(callbackUriString);
            var request = TwitterSignIn.PrepareRequestUserAuthorization(callbackUri, null, null);

            return TwitterSignIn.Channel.PrepareResponse(request).AsNancyResponse();
        }

        [Get("/callback")]
        public dynamic callback(dynamic param)
        {
            logger.Debug("Callback ");
            var response = TwitterSignIn.ProcessUserAuthorization(Request);

            return TwitterSignIn.Channel.PrepareResponse(response).AsNancyResponse();
        }
    }
}

