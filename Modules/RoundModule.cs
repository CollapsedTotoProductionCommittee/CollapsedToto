using System;
using System.Linq;
using StackExchange.Redis;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
        private string CurrentRoundKey = "CurrentRound";

        public RoundModule()
        {
        }

        [Get("/popular")]
        public async Task<dynamic> PopularKeyword(dynamic param, CancellationToken ct)
        {
            SortedSetEntry[] values = await Database.SortedSetRangeByRankWithScoresAsync(CurrentRoundKey, 0, 30);

            return JsonConvert.SerializeObject(values);
        }

        [Post("/bet")]
        public async Task<dynamic> BettingKeyword(dynamic param, CancellationToken ct)
        {
            bool result = false;
            var userID = Session["UserID"];

            if (userID != null)
            {
                dynamic data = Request.Form;
                if (data.Count == 0)
                {
                    data = Request.Query;
                }
                string keyword = (data["keyword"].ToString()).Trim();
                int point = int.Parse(data["point"].ToString());
                using (var context = new UserContext())
                {
                    await Database.SortedSetIncrementAsync(CurrentRoundKey, keyword, 1.0);

                    var userInfo = (from user in context.Users where user.UserID == userID select user).First();
                    userInfo.Point -= point;

                    await context.SaveChangesAsync();

                    result = true;
                }
            }

            return JsonConvert.SerializeObject(result);
        }
    }
}

