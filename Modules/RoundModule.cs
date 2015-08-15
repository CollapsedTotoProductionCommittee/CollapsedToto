using System;
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
            dynamic data = Request.Form;
            if (data.Count == 0)
            {
                data = Request.Query;
            }
            string keyword = (data["keyword"].ToString()).Trim();
            int point = int.Parse(data["point"].ToString());
            await Database.SortedSetIncrementAsync(CurrentRoundKey, keyword, 1.0);

            // TODO: subtract user point
            result = true;

            return JsonConvert.SerializeObject(result);
        }
    }
}

