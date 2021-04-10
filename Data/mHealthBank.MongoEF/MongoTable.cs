using mHealthBank.MongoEF.Interfaces;
using MongoDB.Driver;

namespace mHealthBank.MongoEF
{
    class MongoTable<T> : IMongoTable<T>
        where T : class
    {
        public MongoTable(IClientSessionHandle session, IMongoCollection<T> collection)
        {
            Session = session;
            Collection = collection;
        }

        public IClientSessionHandle Session { get; }
        public IMongoCollection<T> Collection { get; }
    }
}