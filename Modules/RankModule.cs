using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;

namespace CollapsedToto
{
    [Prefix("/rank")]
    public class RankModule : BaseModule
    {
        public RankModule()
        {
        }

        [Get("/point")]
        public dynamic PointRank(dynamic param)
        {
            using (var context = new DatabaseContext())
            {
                var rankers = (from user in context.Users orderby user.Point descending select user).Skip(0).Take(30).ToArray();

                return JsonConvert.SerializeObject(rankers);
            }
        }
    }
}

