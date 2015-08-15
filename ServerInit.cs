using System;
using System.Linq;
using StackExchange.Redis;

namespace CollapsedToto
{
    public static class ServerInit
    {
        public static void Init()
        {
            var redis = RedisContext.Database;

            using (var context = new DatabaseContext())
            {
                redis.KeyDelete(new RedisKey[]
                    {
                        RedisContext.CurrentRoundID,
                        RedisContext.CurrentRoundKey,
                    }
                );

                int roundID = context.RoundResults.Count;
                redis.StringSet(RedisContext.CurrentRoundID, roundID);

                foreach (var user in context.Users)
                {
                }
            }
        }
    }
}

