using AElf.Common.Enums;
using AElf.Configuration;
using Autofac;

namespace AElf.Database
{
    public class DatabaseAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
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
    }
}