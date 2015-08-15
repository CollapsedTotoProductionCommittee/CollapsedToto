﻿using System;
using System.Linq;
using System.ServiceProcess;
using log4net;
using log4net.Config;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Hosting.Self;
using Nancy.Session;
using Nancy.TinyIoc;
using System.Data.Entity;

namespace CollapsedToto
{
    class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            StaticConfiguration.DisableErrorTraces = false;
            MemorySession.Enable(pipelines);
        }
    }

    class Server : ServiceBase
    {
        private NancyHost host = null;
        private ILog logger = null;

        public Server(params string[] hosts)
        {
            logger = LogManager.GetLogger(typeof(Server));
            host = new NancyHost(new Bootstrapper(), (from host in hosts select new Uri(host)).ToArray());
            logger.InfoFormat("Server hosts :\n{0}", String.Join("\n", hosts));

            Database.SetInitializer(new CreateDatabaseIfNotExists<UserContext>());
            using (var context = new UserContext())
            {
                context.Database.Initialize(true);
            }
        }

        public void Start()
        {
            logger.Info("Start Server");
            host.Start();
        }

        protected override void OnStart(string[] args)
        {
            Start();
        }

        public void StopService()
        {
            logger.Info("Stop Server");
            host.Stop();
        }

        protected override void OnStop()
        {
            OnStop();
        }
    }

    static class MainClass
    {
        public static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            Server server = new Server("http://0.0.0.0:8000", "http://127.0.0.1:8000");

#if !DEBUG
            if (args.Count() < 1 || args[0] != "-i")
            {
                ServiceBase.Run(new ServiceBase[]{ server });
            }

            else
#endif
            {
                server.Start();
                Console.WriteLine("Press any key to stop server");
                Console.ReadKey();
                Console.WriteLine("Key pressed, stop server");
                server.StopService();
            }
        }
    }
}
