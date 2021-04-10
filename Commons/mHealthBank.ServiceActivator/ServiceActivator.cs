using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SAGE.IoC;

namespace mHealthBank
{
    public static class ServiceActivator
    {
        private static IDisposableIoC life;

        public static void CreateActivator(this IApplicationBuilder builder)
        {
            life = builder.ApplicationServices.GetService<IDisposableIoC>();
        }

        public static IDisposableIoC GetActivator()
        {
            return life;
        }
    }
}