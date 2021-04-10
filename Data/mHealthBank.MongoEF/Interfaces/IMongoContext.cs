using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace mHealthBank.MongoEF.Interfaces
{
    interface IMongoContext : IDisposable
    {
        void AddCommand(Func<Task> func);
        Task<int> SaveChanges();
        //IMongoDatabase GetDatabase();
        //IMongoClient GetClient();
        Task ClearChanges();

        IMongoTable<T> GetCollection<T>() where T : class;
        Task<IClientSessionHandle> CreateTransaction();
        void DestroyTransaction();
        IClientSessionHandle GetActiveTransaction();
        bool SupportReplicate();
    }
}