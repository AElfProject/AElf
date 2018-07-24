using AElf.Configuration;
using AElf.Database;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class DatabaseModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            switch (DatabaseConfig.Instance.Type)
            {
                case DatabaseType.InMemory:
                    builder.RegisterType<KeyValueDatabase>().As<IKeyValueDatabase>().SingleInstance();
                    break;
                case DatabaseType.Redis:
                    builder.RegisterType<RedisDatabase>().As<IKeyValueDatabase>().SingleInstance();
                    break;
                case DatabaseType.Ssdb:
#if DEBUG
                    if (!new SsdbDatabase().IsConnected())
                    {
                        builder.RegisterType<KeyValueDatabase>().As<IKeyValueDatabase>().SingleInstance();
                        break;
                    }
#endif
                    builder.RegisterType<SsdbDatabase>().As<IKeyValueDatabase>().SingleInstance();
                    break;
            }
        }
    }
}