using System;
using Nancy.Session;

namespace CollapsedToto
{
    public class BaseLogic
    {
        protected ISession m_srcSession;

        public BaseLogic(ISession webSesson)
        {
            m_srcSession = webSesson;
        }

        public string UserID
        {
            get
            {
                return (string)m_srcSession["UserID"];
            }
        }

        public bool IsLoggedIn
        {
            get
            {
                return m_srcSession["UserID"] != null;
            }
        }

        public string UserProfilePathPrefix
        {
            get
            {
                return "/user/";
            }
        }

        public string LeaderboardPathPrefix
        {
            get
            {
                return "/rank/point";
            }
        }
    }
}

