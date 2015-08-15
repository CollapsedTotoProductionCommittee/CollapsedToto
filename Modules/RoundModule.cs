using System;
using System.Linq;
using StackExchange.Redis;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CollapsedToto
{
    [Prefix("/round")]
    public class RoundModule : BaseModule
    {
        private static ConnectionMultiplexer redis = null;
        private static IDatabase Database
        {
            get
            {
                if (redis == null)
                {
                    redis = ConnectionMultiplexer.Connect("127.0.0.1:6379,resolveDns=True");
                }

                return redis.GetDatabase();
            }
        }
        private string CurrentRoundID = "CurrentRoundID";
        private string CurrentRoundKey = "CurrentRound";
        private string CurrentRoundPoint = "CurrentRountPoint";
        private string CurrentRoundBettingCount = "CurrentRoundBettingCount";

        public RoundModule()
        {
        }

        [Get("/popular")]
        public async Task<dynamic> PopularKeyword(dynamic param, CancellationToken ct)
        {
            SortedSetEntry[] values = await Database.SortedSetRangeByRankWithScoresAsync(CurrentRoundKey, 0, 30);

            return JsonConvert.SerializeObject(values);
        }

        [Get("/current")]
        public async Task<dynamic> CurrentRoundInfo(dynamic param, CancellationToken ctor)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();

            ret.Add("totalPoint", await Database.StringGetAsync(CurrentRoundPoint));
            ret.Add("totalCount", await Database.StringGetAsync(CurrentRoundBettingCount));

            return JsonConvert.SerializeObject(ret);
        }

        [Post("/bet")]
        public async Task<dynamic> BettingKeyword(dynamic param, CancellationToken ct)
        {
            bool result = false;
            string userID = Session["UserID"].ToString();

            if (userID != null)
            {
                dynamic data = Request.Form;
                if (data.Count == 0)
                {
                    data = Request.Query;
                }
                string keyword = (data["keyword"].ToString()).Trim();
                int point = int.Parse(data["point"].ToString());

                // Special case.
                if (keyword.Length == 0)
                {
                    // ...
                }
                else
                {
                    int totalLength = keyword.Length;
                    int whiteSpaceCount = keyword.Count(c => char.IsWhiteSpace(c));
                    if ((whiteSpaceCount != 0 || totalLength < 2) && (totalLength - whiteSpaceCount) < 3)
                    {
                        return JsonConvert.SerializeObject(new Dictionary<string, object>
                            {
                                { "result", false },
                                { "reason", "Too short" }
                            }
                        );
                    }
                }

                int roundID = int.Parse(Database.StringGet(CurrentRoundID));

                using (var context = new DatabaseContext())
                {
                    await Database.SortedSetIncrementAsync(CurrentRoundKey, keyword, 1.0);
                    await Database.StringIncrementAsync(CurrentRoundPoint, point);

                    var userInfo = (from user in context.Users
                                                  where user.UserID.Equals(userID)
                                                  select user).First();
                    userInfo.Point -= point;

                    UserRound roundInfo = (from round in context.UserRounds
                                                          where round.Owner == userInfo && round.RoundID == roundID
                                                          select round).FirstOrDefault();
                    if (roundInfo == null)
                    {
                        roundInfo = new UserRound
                        {
                                Owner = userInfo,
                                RoundID = roundID
                        };
                    }

                    if (roundInfo.BettedWords.ContainsKey(keyword))
                    {
                        roundInfo.BettedWords[keyword] += point;
                    }
                    else
                    {
                        roundInfo.BettedWords.Add(keyword, point);
                        await Database.StringIncrementAsync(CurrentRoundBettingCount);
                    }

                    await context.SaveChangesAsync();

                    result = true;
                }
            }

            return JsonConvert.SerializeObject(new Dictionary<string, object>
                {
                    { "result", result }
                }
            );
        }
    }
}

