using System;
using Nancy;
using System.Threading.Tasks;
using System.Threading;

namespace CollapsedToto
{
    public class MainModule : BaseModule
    {
        public MainModule()
        {
            
        }

        [Get("/")]
        public dynamic index(dynamic param)
        {
            return View["index", new BaseLogic(Session)];
        }

        [Get("/about")]
        public dynamic about(dynamic param)
        {
            return View["about", new BaseLogic(Session)];
        }

        [Get("/my")]
        public dynamic mypage(dynamic param)
        {
            return View["mypage", new BaseLogic(Session)];
        }
    }
}

