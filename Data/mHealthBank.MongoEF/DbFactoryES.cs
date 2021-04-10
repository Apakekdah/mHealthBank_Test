using mHealthBank.MongoEF.Interfaces;
using SAGE.Core.Commons;
using SAGE.IoC;

namespace mHealthBank.MongoEF
{
    class DbFactoryES : Disposable, IDbFactoryES
    {
        private readonly IDisposableIoC life;
        private IMongoContext dataContext;

        public DbFactoryES(IDisposableIoC life)
        {
            this.life = life;
        }

        public IMongoContext Get()
        {
            return (dataContext ??= life.GetInstance<IMongoContext>());
        }

        protected override void DisposeCore()
        {
            if (dataContext != null)
                dataContext.Dispose();
        }
    }
}