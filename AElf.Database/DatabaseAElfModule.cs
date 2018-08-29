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
            switch (DatabaseConfig.Instance.Type)
            {
                case DatabaseType.InMemory:
                    builder.RegisterType<InMemoryDatabase>().As<IKeyValueDatabase>().SingleInstance();
                    break;
                case DatabaseType.Redis:
                    builder.RegisterType<RedisDatabase>().As<IKeyValueDatabase>().SingleInstance();
                    break;
                case DatabaseType.Ssdb:
                    builder.RegisterType<SsdbDatabase>().As<IKeyValueDatabase>().SingleInstance();
                    break;
            }
        }

        public void Run(IContainer container)
        {
            var db = container.Resolve<IKeyValueDatabase>();
            var result = db.IsConnected();
            if (!result)
            {
                throw new Exception("failed to connect database");
            }
        }
    }
}