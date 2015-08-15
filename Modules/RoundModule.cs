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
        public RoundModule()
        {
        }

        [Get("/popular")]
        public async Task<dynamic> PopularKeyword(dynamic param, CancellationToken ct)
        {
            SortedSetEntry[] values = await RedisContext.Database.SortedSetRangeByRankWithScoresAsync(RedisContext.CurrentRoundKey, 0, 30);

            return JsonConvert.SerializeObject(values);
        }

        [Get("/current")]
        public async Task<dynamic> CurrentRoundInfo(dynamic param, CancellationToken ctor)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();

            ret.Add("totalPoint", await RedisContext.Database.StringGetAsync(RedisContext.CurrentRoundPoint));
            ret.Add("totalCount", await RedisContext.Database.StringGetAsync(RedisContext.CurrentRoundBettingCount));

            return JsonConvert.SerializeObject(ret);
        }

        [Get("/bet")]
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

                int roundID = int.Parse(RedisContext.Database.StringGet(RedisContext.CurrentRoundID));

                using (var context = new DatabaseContext())
                {
                    await RedisContext.Database.SortedSetIncrementAsync(RedisContext.CurrentRoundKey, keyword, 1.0);
                    await RedisContext.Database.StringIncrementAsync(RedisContext.CurrentRoundPoint, point);

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
                        await RedisContext.Database.StringIncrementAsync(RedisContext.CurrentRoundBettingCount);
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

