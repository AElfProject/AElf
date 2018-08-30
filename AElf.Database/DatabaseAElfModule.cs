using System;
using AElf.Common.Enums;
using AElf.Common.Module;
using AElf.Configuration;
using Autofac;

namespace AElf.Database
{
    public class DatabaseAElfModule : IAElfModlule
    {
        public void Init(ContainerBuilder builder)
        {
            builder.RegisterModule(new DatabaseAutofacModule());
        }

        public void Run(ILifetimeScope scope)
        {
            var db = scope.Resolve<IKeyValueDatabase>();
            var result = db.IsConnected();
            if (!result)
            {
                throw new Exception("failed to connect database");
            }
        }
    }
}