using mHealthBank.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SAGE.Core.Interface;
using SAGE.IoC;

namespace mHealthBank.ApiController
{
    public class ControllerApiBase : ControllerBase
    {
        protected readonly IDisposableIoC life;
        protected readonly IMappingObject mapping;
        protected ILogger Log { get; }

        const string ASSUME_SESSION_LOGIN_USER = "test";

        public ControllerApiBase(IDisposableIoC life)
        {
            this.life = life;
            mapping = life.GetInstance<IMappingObject>();

            Log = life.GetInstance<ILogger>(new KeyValueParameter("type", this.GetType()));
        }

        protected virtual string GetActiveUser()
        {
            return ASSUME_SESSION_LOGIN_USER;
        }

    }
}
