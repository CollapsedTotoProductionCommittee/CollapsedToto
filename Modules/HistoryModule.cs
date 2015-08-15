using System;

namespace CollapsedToto
{
    [Prefix("/history")]
    public class HistoryModule : BaseModule
    {
        [Get("/")]
        public dynamic History(dynamic param)
        {
            return View["history", new BaseLogic(Session)];
        }

        [Get("/round")]
        public dynamic RoundHistory(dynamic param)
        {
            return "Empty";
        }
    }
}

