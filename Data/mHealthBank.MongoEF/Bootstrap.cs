using mHealthBank.Entities;
using mHealthBank.MongoEF;
using mHealthBank.MongoEF.Context;
using mHealthBank.MongoEF.Interfaces;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson.Serialization.Conventions;
using SAGE.IoC;
using System.Collections.Concurrent;

namespace mHealthBank
{
    public static class Bootstrap
    {
        internal const string TABLE_MAPS = "tableMaps";

        public static void RegisterEF(this IBuilderIoC builder, IConfiguration configuration)
        {

            builder.Register<IMongoContext, MongoContext>(ScopeIoC.Lifetime);
            builder.RegisterAsImplement<DbFactoryES>(ScopeIoC.Lifetime);
            builder.RegisterAsImplement<UoW>(ScopeIoC.Lifetime);

            // Register Entities
            builder
                /// Scoring
                .RegisterAsImplement<RepositoryBase<Customer>>(ScopeIoC.Lifetime)
                ;

            builder.RegisterGeneric(typeof(MongoTable<>), typeof(IMongoTable<>), ScopeIoC.Lifetime);

            builder.RegisterAsSelf<ConcurrentBag<string>>(TABLE_MAPS, ScopeIoC.Singleton);

            MongoDBConvetion();

            builder.Register(fn =>
            {
                return new MongoTabelRunning();
            }, ScopeIoC.Singleton);
        }

        private static void MongoDBConvetion()
        {
            var convention = new ConventionPack
            {
                new EnumRepresentationConvention(MongoDB.Bson.BsonType.String),
                new CamelCaseElementNameConvention()
            };
            ConventionRegistry.Register("All", convention, _ => true);
        }
    }
}
