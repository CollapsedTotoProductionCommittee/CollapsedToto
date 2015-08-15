using System;
using System.Linq;
using StackExchange.Redis;
using System.Collections.Generic;

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
                        RedisContext.UserPointRank
                    }
                );

                int roundID = context.RoundResults.Count();
                redis.StringSet(RedisContext.CurrentRoundID, roundID);

                List<SortedSetEntry> entries = new List<SortedSetEntry>();
                foreach (var user in context.Users)
                {
                    entries.Add(new SortedSetEntry(user.UserID, user.Point));
                }
                redis.SortedSetAdd(RedisContext.UserPointRank, entries.ToArray());
            }
        }
    }
}

