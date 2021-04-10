using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Web;
using SAGE.IoC;
using SAGE.Logger.NLog;
using System;

namespace mHealthBank.LogRegistration.Logger.NLogFactory
{
    internal static class NLogProvider
    {
        private static LogFactory factory;

        public static void InitLog(string fileName)
        {
            factory = NLogBuilder.ConfigureNLog(fileName);
            factory.AutoShutdown = true;
        }

        public static void ShutDown()
        {
            //LogManager.Flush();
            //LogManager.Shutdown();
            factory.Flush();
            factory.Shutdown();
        }

        public static SAGE.Core.Interface.ILogger GetLogger<T>()
            where T : class
        {
            return GetLogger(typeof(T));
        }

        public static SAGE.Core.Interface.ILogger GetLogger(Type type)
        {
            SAGE.Core.Interface.ILogger log;
            var life = ServiceActivator.GetActivator();
            if (life == null)
            {
                if (factory == null)
                    throw new Exception("Log Must Be Intiate first");

                log = new LogIt(type);
            }
            else
            {
                log = life.GetInstance<SAGE.Core.Interface.ILogger>(new KeyValueParameter("type", type));
            }
            return log;
        }

        public static IWebHostBuilder ActivateNLog(IWebHostBuilder builder)
        {
            return builder.UseNLog();
        }

        public static IHostBuilder ActivateNLog(IHostBuilder builder)
        {
            return builder.UseNLog();
        }
    }
}
