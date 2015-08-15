using System;
using StackExchange.Redis;

namespace CollapsedToto
{
    public static class RedisContext
    {
        public static string CurrentRoundID = "CurrentRoundID";
        public static string CurrentRoundKey = "CurrentRound";
        public static string CurrentRoundPoint = "CurrentRountPoint";
        public static string CurrentRoundBettingCount = "CurrentRoundBettingCount";

        private static ConnectionMultiplexer redis = null;

        public static IDatabase Database
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
    }
}

