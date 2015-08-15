using System;
using System.Collections.Generic;
using System.Collections;

namespace CollapsedToto
{
    static public class Constants
    {
        static public string TwitterConsumerSecret = "";
        static public TimeSpan MinimumDelay = new TimeSpan(0, 30, 0);
        static public int MinimumPoint = 100;

        static public string TwitterConsumerKey = "0MoZc9Bu0KcFeWYMV90ckIQoX";
        static public string TwitterAccessToken = "";
        static public string TwitterAccessSecret = "";
        static public string CollapsedBotId = "342104633";

        public class BonusTable : IEnumerable
        {
            SortedDictionary<int, double> data;
            double defaultValue;

            public BonusTable(double defaultVal = 1.0)
            {
                defaultValue = defaultVal;
            }

            public IEnumerator GetEnumerator()
            {
                return data.GetEnumerator();
            }

            public void Add(int idx, double val)
            {
                data.Add(idx, val);
            }

            public double this[int idx]
            {
                get
                {
                    double prevVal = defaultValue;
                    foreach (var pair in data)
                    {
                        if (pair.Key > idx)
                        {
                            return prevVal;
                        }

                        prevVal = pair.Value;
                    }

                    return prevVal;
                }
            }
        }
    }
}

