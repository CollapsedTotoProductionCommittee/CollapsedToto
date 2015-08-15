using System;
using Nancy.Session;
using Nancy;
using Nancy.Bootstrapper;
using System.Collections.Generic;
using Nancy.Cookies;
using System.Linq;
using System.Text;
using System.Collections;

namespace CollapsedToto
{
    public class MemorySession : IObjectSerializerSelector
    {
        protected string cookieName = "SESSIONID";


        public MemorySession()
        {
            store = new Dictionary<string, Dictionary<string, object>>();
        }

        public void WithSerializer(IObjectSerializer newSerializer)
        {
            return;
        }

        ISession Load(Request request)
        {
            if (request.Cookies.ContainsKey(cookieName))
            {
                string sessionID = Uri.UnescapeDataString(request.Cookies[cookieName]);
                if (store.ContainsKey(sessionID))
                {
                    return new Session(store[sessionID]);
                }
            }
            return new Session(new Dictionary<string, object>());
        }

        public void Save(ISession session, Response response)
        {
            if (session == null || !session.HasChanged)
            {
                return;
            }

            string sessionID = null;

            if (!response.Cookies.Any(cookie => cookie.Name == cookieName))
            {
                sessionID = Convert.ToBase64String(Encoding.UTF8.GetBytes(DateTime.UtcNow.ToLongTimeString()));
                response.WithCookie(new NancyCookie(cookieName, sessionID));
            }
            else
            {
                sessionID = response.Cookies.Where(cookie => cookie.Name == cookieName).Select(cookie => cookie.Value).First();
            }

            store[sessionID] = session.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        protected Dictionary<string, Dictionary<string, object>> store;

        static Response InitSession(NancyContext ctx, MemorySession sessionStore)
        {
            if (ctx.Request == null)
            {
                return null;
            }

            ctx.Request.Session = sessionStore.Load(ctx.Request);
            ctx.Request.Session["Logic"] = new BaseLogic(ctx.Request.Session);

            return null;
        }

        static void SaveSession(NancyContext ctx, MemorySession sessionStore)
        {
            ctx.Request.Session["Logic"] = null;
            sessionStore.Save(ctx.Request.Session, ctx.Response);
        }

        public static IObjectSerializerSelector Enable(IPipelines pipelines)
        {
            if (pipelines == null)
            {
                throw new ArgumentNullException("pipelines");
            }

            var sessionStore = new MemorySession();

            pipelines.BeforeRequest.AddItemToStartOfPipeline(ctx => InitSession(ctx, sessionStore));
            pipelines.AfterRequest.AddItemToEndOfPipeline(ctx => SaveSession(ctx, sessionStore));

            return sessionStore;
        }
    }
}

