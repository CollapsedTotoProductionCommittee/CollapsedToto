using System;
using System.Threading.Tasks;
using System.Threading;

namespace CollapsedToto
{
    [Prefix("/user")]
    public class UserModule : BaseModule
    {
        public UserModule()
        {
        }

        [Get("/signin")]
        public async Task<dynamic> signin(dynamic param, CancellationToken token)
        {
            return "SignIn";
        }
    }
}

