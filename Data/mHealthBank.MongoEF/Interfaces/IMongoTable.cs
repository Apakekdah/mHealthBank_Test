using MongoDB.Driver;

namespace mHealthBank.MongoEF.Interfaces
{
    interface IMongoTable<T> where T : class
    {
        IClientSessionHandle Session { get; }
        IMongoCollection<T> Collection { get; }
    }
}