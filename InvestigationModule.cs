using System;
using System.Threading;
using DotNetOpenAuth.ApplicationBlock;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth;
using DotNetOpenAuth.OAuth.Messages;
using System.Collections.Generic;
using System.Net;
using System.IO;
using log4net;

namespace CollapsedToto
{
    public class InvestigationModule
    {
        protected Thread thread = null;
        public InvestigationModule()
        {
            Investigator investigator = new Investigator();
            thread = new Thread(investigator.Investigate);
        }

        internal class Investigator
        {
            WebConsumer consumer = null;
            ILog logger = null;
            bool doInvestigate = true;

            public Investigator()
            {
                logger = LogManager.GetLogger(typeof(Investigator)); 
                InMemoryTokenManager tokenManager = new InMemoryTokenManager(Constants.TwitterConsumerKey, Constants.TwitterConsumerSecret);
                tokenManager.StoreAccessToken(Constants.TwitterAccessToken, Constants.TwitterAccessSecret);
                consumer = new WebConsumer(UserModule.ServiceDescription, tokenManager);
            }

            ~Investigator()
            {
                if (req != null)
                {
                    req.Abort();
                    res.Close();
                    reader.Close();
                }
            }

            public void Investigate()
            {
                MessageReceivingEndpoint endpoint = new MessageReceivingEndpoint(
                    "https://stream.twitter.com/1.1/statuses/filter.json",
                    HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest);

                var extraData = new Dictionary<string, string>
                {
                    { "follow", Constants.CollapsedBotId }
                };
                req = consumer.PrepareAuthorizedRequest(endpoint, Constants.TwitterAccessToken, extraData);
                req.BeginGetResponse(this.OnResponse, req);
            }

            HttpWebRequest req = null;
            WebResponse res = null;
            StreamReader reader = null;
            public void OnResponse(IAsyncResult ar)
            {
                res = req.EndGetResponse(ar);

                Stream stream = res.GetResponseStream();
                reader = new StreamReader(stream);

                while(doInvestigate)
                {
                    string line = reader.ReadLineAsync().Result;
                    logger.Debug(line);
                }
            }
        }

        public void Start()
        {
            thread.Start();
        }

        public void Stop()
        {
        }
    }
}

