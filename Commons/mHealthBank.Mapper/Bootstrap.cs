using AutoMapper;
using mHealthBank.Interfaces;
using SAGE.IoC;

namespace mHealthBank
{
    public static class Bootstrap
    {
        public static void RegisterMapper(this IBuilderIoC builder, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            var mapConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfile(new Mapper.Mapper());
            });

            var imap = mapConfig.CreateMapper();

            builder.RegisterSingleton(imap);

            builder.Register<IMappingObject, Mapper.MappingObject>(ScopeIoC.Singleton);
        }
    }
}