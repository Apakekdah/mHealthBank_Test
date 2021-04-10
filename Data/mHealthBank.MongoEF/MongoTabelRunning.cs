using mHealthBank.MongoEF.Context;
using mHealthBank.MongoEF.Interfaces;
using SAGE.Core.Commons;
using System;
using System.Linq;
using System.Reflection;

namespace mHealthBank.MongoEF
{
    class MongoTabelRunning
    {
        bool isRunning;

        public MongoTabelRunning()
        {
            Run();
        }

        public void Run()
        {
            if (isRunning) return;

            isRunning = true;

            Assembly[] asms = new[] { this.GetType().Assembly };
            Type intfs = typeof(IMongoTableConfigurable<>);

            asms.Each(asm =>
            {
                asm.GetTypes().Where(c => c.IsClass &&
                                intfs.IsClosesAsType(c) &&
                                !typeof(MongoTableConfigurator<>).Name.Equals(c.Name))
                    .Each(cls =>
                    {
                        var meth = cls.GetMethod("Apply");
                        if (meth != null)
                        {
                            var instance = Activator.CreateInstance(cls);
                            meth.Invoke(instance, null);
                        }
                    });
            });
        }
    }
}