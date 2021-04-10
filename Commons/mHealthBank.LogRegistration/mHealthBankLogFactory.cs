using mHealthBank.LogRegistration.Logger.NLogFactory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using Topshelf;
using Topshelf.HostConfigurators;

namespace mHealthBank
{
    public static class mHealthBankLogFactory
    {
        public static void InitLog(string fileName)
        {
            NLogProvider.InitLog(fileName);
        }

        public static void ShutDown()
        {
            NLogProvider.ShutDown();
        }

        public static SAGE.Core.Interface.ILogger GetLogger<T>()
            where T : class
        {
            return NLogProvider.GetLogger<T>();
        }

        public static SAGE.Core.Interface.ILogger GetLogger(Type type)
        {
            return NLogProvider.GetLogger(type);
        }

        public static IWebHostBuilder UseEagleScoreLog(this IWebHostBuilder builder)
        {
            return NLogProvider.ActivateNLog(builder);
        }

        public static IHostBuilder UseEagleScoreLog(this IHostBuilder builder)
        {
            return NLogProvider.ActivateNLog(builder);
        }

        public static void UseEagleScoreTopShelfLog(this HostConfigurator configurator)
        {
            configurator.UseNLog();
        }
    }
}
