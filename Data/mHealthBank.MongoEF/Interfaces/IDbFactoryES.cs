using SAGE.Core.Interface;

namespace mHealthBank.MongoEF.Interfaces
{
    interface IDbFactoryES : IDatabaseFactory<IMongoContext>
    {
    }
}