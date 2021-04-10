using mHealthBank.Models.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SAGE.IoC;
using SAGE.IoC.Autofac;
using SAGE.Logger.NLog;

namespace mHealthBank
{
    public static class ServiceRegistration
    {
        public static void RegisterRequiredDevelopment(this IBuilderIoC builder, IConfiguration configuration)
        {
            BuilderRegistration(builder, configuration, true);
        }

        public static void RegisterRequired(this IBuilderIoC builder, IConfiguration configuration)
        {
            BuilderRegistration(builder, configuration, false);
        }

        private static void BuilderRegistration(this IBuilderIoC builder, IConfiguration configuration, bool isDevelopment)
        {
            builder.RegisterModule<NLogModule>();

            builder.Register<SAGE.Core.Interface.ILogger, LogIt>(ScopeIoC.Lifetime);

            builder.RegisterEF(configuration);
            builder.RegisterBusiness(configuration);
            builder.RegisterMapper(configuration);
        }

        public static void RegisterConfiguration(this IServiceCollection service, IConfiguration configuration)
        {
            service.Configure<CustomerConfiguration>(configuration.GetSection(CustomerConfiguration._ConfigurationName));
        }
    }
}
