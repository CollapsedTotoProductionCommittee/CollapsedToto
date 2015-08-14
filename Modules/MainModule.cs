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
            return View["index"];
        }
    }
}

