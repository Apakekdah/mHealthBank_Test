using mHealthBank.MongoEF.Interfaces;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace mHealthBank.MongoEF
{
    class UoW : IUnitOfWorkMongo
    {
        protected IDbFactoryES DatabaseFactory { get; set; }

        protected IClientSessionHandle Transaction { get; set; }

        protected bool IsTransaction { get; set; }

        protected IMongoContext DataContext
        { get { return DatabaseFactory.Get(); } }

        public UoW(IDbFactoryES dbFactory)
        {
            DatabaseFactory = dbFactory;
        }

        public async Task Begin()
        {
            if (!DataContext.SupportReplicate()) return;
            else if (IsTransaction) return;

            try
            {
                Transaction = await DataContext.CreateTransaction().ConfigureAwait(false);

                Transaction.StartTransaction();
                IsTransaction = true;
            }
            catch (Exception)
            {
                // handle
            }
        }

        public async Task<int> Commit()
        {
            return await Commit(true);
        }

        public async Task<int> Commit(bool postToDb)
        {
            int changes = 0;

            try
            {
                changes = await DataContext.SaveChanges();

                if (postToDb)
                    await CommitTransaction();
                else
                    await Rollback();
            }
            catch (Exception)
            {
                ClearTransaction(true);
                throw;
            }
            finally
            {
                // Belum tau buat apa.
            }

            return changes;
        }

        public async Task Rollback()
        {
            await DataContext.ClearChanges();

            if (!IsTransaction)
                await Task.FromResult(0).ConfigureAwait(false); //return Task.Run(() => true);

            ClearTransaction(true);
        }

        private async Task CommitTransaction()
        {
            if (!IsTransaction)
                return;

            try
            {
                await Transaction.CommitTransactionAsync();
            }
            finally
            {
                ClearTransaction();
            }
        }

        private void ClearTransaction(bool abort = false)
        {
            if (!IsTransaction) return;

            try
            {
                IsTransaction = false;
                if (Transaction != null)
                {
                    if (Transaction.IsInTransaction)
                    {
                        try
                        {
                            if (abort)
                                Transaction.AbortTransaction();
                        }
                        finally
                        {
                            //
                        }
                    }

                    DataContext.DestroyTransaction();

                    Transaction?.Dispose();
                }
            }
            finally
            {
                Transaction = null;
            }
        }
    }
}