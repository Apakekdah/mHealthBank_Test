using Microsoft.Extensions.Configuration;
using SAGE.IoC;

namespace mHealthBank
{
    public static class Bootstrap
    {
        public static void RegisterBusiness(this IBuilderIoC builder, IConfiguration configuration)
        {
            var asm = typeof(FakeClass).Assembly;
            builder.RegisterAssemblyTypes(RegistrationTypeIoC.AsLook,
                new[] { typeof(SAGE.Business.IBusinessClassAsync<>) }, ScopeIoC.Lifetime,
                new[] { asm });
        }
    }

    class FakeClass
    {
    }
}