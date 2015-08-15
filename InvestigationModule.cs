using System;
using System.Threading;
using DotNetOpenAuth.ApplicationBlock;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth;
using DotNetOpenAuth.OAuth.Messages;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.IO;
using log4net;
using Newtonsoft.Json.Linq;

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
                    line = line.Trim();
                    if (line.Length > 0)
                    {
                        ProcessNewTweet(line);
                    }
                }
            }

            public void ProcessNewTweet(string line)
            {
                JObject tweet = JObject.Parse(line);

                // 트윗 삭제는 무시
                if (tweet["deleted"] == null)
                {
                    return;
                }

                string text = tweet["text"].ToString();
                DateTime now = DateTime.Parse(tweet["created_at"].ToString());
                using (var context = new DatabaseContext())
                {
                    RoundResult lastResult = context.RoundResults.LastOrDefault();
                    int newRoundID = 1;
                    if (lastResult != null)
                    {
                        if (now - lastResult.TweetTime < Constants.MinimumDelay)
                        {
                            return;
                        }
                        newRoundID = lastResult.RoundID + 1;
                    }

                    RoundResult newResult = new RoundResult();
                    newResult.RoundID = newRoundID;
                    newResult.Text = text;
                    newResult.TweetTime = now;

                    var redis = RedisContext.Database;
                    redis.StringSet(RedisContext.CurrentRoundID, newRoundID);
                    var keywords = redis.SortedSetRangeByRankWithScores(RedisContext.CurrentRoundKey);
                    KeyValuePair<string, int>? specialBet = null;
                    int matchedPoint = 0, unmatchedPoint = 0;
                    foreach (var keyword in keywords)
                    {
                        var pair = new KeyValuePair<string, int>(keyword.Element.ToString(), (int)keyword.Score);
                        if (pair.Key.Length == 0)
                        {
                            specialBet = pair;
                            continue;   
                        }

                        if (text.Contains(pair.Key))
                        {
                            newResult.MatchedValues.Add(pair);
                            matchedPoint += pair.Value;
                        }
                        else
                        {
                            newResult.UnmatchedValues.Add(pair);
                            unmatchedPoint += pair.Value;
                        }
                    }

                    if (specialBet != null)
                    {
                        if (newResult.MatchedValues.Count == 0)
                        {
                            newResult.MatchedValues.Add(specialBet.Value);
                            matchedPoint += specialBet.Value.Value;
                        }
                        else
                        {
                            newResult.UnmatchedValues.Add(specialBet.Value);
                            unmatchedPoint += specialBet.Value.Value;
                        }
                    }

                    context.RoundResults.Add(newResult);
                    context.SaveChanges();
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

