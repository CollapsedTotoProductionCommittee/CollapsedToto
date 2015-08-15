using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DotNetOpenAuth;
using DotNetOpenAuth.ApplicationBlock;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth;
using DotNetOpenAuth.OAuth.ChannelElements;
using DotNetOpenAuth.OAuth.Messages;
using log4net;
using Nancy;
using Nancy.Responses;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            Dictionary<string, string> redirectParam = null;
            if (Request.Query.redirect != null)
            {
                redirectParam = new Dictionary<string, string>
                {
                    { "redirect", Request.Query.redirect }
                };
            }
            var callbackUri = new Uri(callbackUriString);
            var request = TwitterSignIn.PrepareRequestUserAuthorization(callbackUri, null, redirectParam);

            return TwitterSignIn.Channel.PrepareResponse(request).AsNancyResponse();
        }

        [Get("/callback")]
        public async Task<dynamic> callback(dynamic param, CancellationToken ct)
        {
            logger.Debug("Callback ");
            var response = TwitterSignIn.ProcessUserAuthorization(Request);
            string userID = response.ExtraData["user_id"];
            string screenName = response.ExtraData["screen_name"];
            Session["UserID"] = userID;

            MessageReceivingEndpoint endpoint = new MessageReceivingEndpoint(
                "https://api.twitter.com/1.1/users/show.json",
                HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest);

            var extraData = new Dictionary<string, string>
            {
                { "user_id", userID },
                { "screen_name", screenName }
            };
            HttpWebRequest req = TwitterSignIn.PrepareAuthorizedRequest(endpoint, response.AccessToken, extraData);
            req.BeginGetResponse(this.OnProfile, new Tuple<User, HttpWebRequest>(
                new User(userID)
                {
                    ScreenName = screenName
                },
                req
            ));

            string redirect = "/";
            if (Request.Query.redirect != null)
            {
                redirect = Request.Query.redirect;
            }

            return new RedirectResponse(redirect);
        }

        [Get("/info/{id}")]
        public dynamic UserInfo(dynamic param)
        {
            using (var context = new DatabaseContext())
            {
                string userID = param.id.ToString();
                return JsonConvert.SerializeObject(context.Users.Where(u => u.UserID.Equals(userID)).FirstOrDefault());
            }
        }

        [Post("/revive")]
        public dynamic ReviveRequest(dynamic param)
        {
            bool ret = false;
            using (var context = new DatabaseContext())
            {
                User user = context.Users.Where(u => u.UserID.Equals((string)Session["UserID"])).FirstOrDefault<User>();

                if (user.Point <= Constants.MinimumPoint)
                {
                    TimeSpan reviveTerm = new TimeSpan(0, user.PaneltyLevel ^ 2 * 5, 0);
                    DateTime now = DateTime.UtcNow;

                    if (user.ReviveRequestedTime.Year == 1970 || user.ReviveRequestedTime + reviveTerm < now)
                    {
                        user.ReviveRequestedTime = now;

                        context.SaveChanges();

                        ret = true;
                    }
                }
            }

            return JsonConvert.SerializeObject(ret);
        }

        [Post("/upgrade")]
        public dynamic Upgrade(dynamic param)
        {
            bool ret = false;
            using (var context = new DatabaseContext())
            {
                User user = context.Users.Where(u => u.UserID.Equals((string)Session["UserID"])).FirstOrDefault();

                int requiredPoint = user.UpgradeCount ^ 2 * 200;
                if (user.Point > requiredPoint)
                {
                    user.UpgradeCount += 1;
                    --user.PaneltyLevel;

                    context.SaveChanges();

                    ret = true;
                }
            }

            return JsonConvert.SerializeObject(ret);
        }

        public void OnProfile(IAsyncResult ar)
        {
            Tuple<User, HttpWebRequest> state = ar.AsyncState as Tuple<User, HttpWebRequest>;
            User user = state.Item1 as User;
            HttpWebRequest req = state.Item2 as HttpWebRequest;
            JObject res = JObject.Parse(new StreamReader(req.EndGetResponse(ar).GetResponseStream()).ReadToEnd());
            user.UserFullName = res["name"].ToString();
            user.ProfileIconURL = res["profile_image_url"].ToString();
            using (var context = new DatabaseContext())
            {
                if (!context.Users.Any(u => u.UserID == user.UserID))
                {
                    context.Users.Add(user);
                }
                else
                {
                    context.Users.Attach(user);
                }
                context.SaveChangesAsync();
            }
        }
    }
}

