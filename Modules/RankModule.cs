using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Nancy.Session;
using System.Text;
using System.Collections.Generic;

namespace CollapsedToto
{
    [Prefix("/rank")]
    public class RankModule : BaseModule
    {
        public RankModule()
        {
        }

        [Get("/point/{page:int}")]
        public dynamic PointLeaderBoard(dynamic param)
        {
            return View["leaderboard", new RankLogic(Session, param.page, Session["UserID"].ToString())];
        }

        public class UserInfo
        {
            public UserInfo(string ui, string un)
            {
                userId = ui;
                userName = un;
            }

            public int rank { get; set; }
            public string userId { get; set; }
            public string userName { get; set ; }
            public int Point { get; set; }
        }
    }

    public class RankLogic : BaseLogic
    {
        const int navigationPageSize = 10;
        const int pageSize = 20;

        int page;
        string UserID;

        public RankLogic(ISession session, int page, string userId)
            : base(session)
        {
            this.page = page;
        }

        public int UserOffset
        {
            get
            {
                var userRank = RedisContext.Database.SortedSetRank(RedisContext.CurrentRoundPoint, UserID);
                if (!userRank.HasValue)
                {
                    return -1;
                }
                else
                {
                    return (int)userRank.Value / pageSize;
                }
            }
        }

        public string CurrentList
        {
            get
            {
                using (var context = new DatabaseContext())
                {
                    var userInfos = (from user in context.Users orderby user.Point descending
                        select new CollapsedToto.RankModule.UserInfo(user.UserID, user.UserFullName)).Skip((int)page * 20).Take((int)(page + 1) * 20).ToArray();

                    int index = page * 20;;

                    foreach (var userInfo in userInfos)
                    {
                        userInfo.rank = index;
                        ++index;
                    }

                    return JsonConvert.SerializeObject(userInfos);
                }
            }
        }

        public int CurrentPage
        {
            get
            {
                return page;
            }
        }

        public int TotalPage
        {
            get
            {
                using (var context = new DatabaseContext())
                {
                    return context.Users.Count() / pageSize;
                }
            }
        }
    }
}

