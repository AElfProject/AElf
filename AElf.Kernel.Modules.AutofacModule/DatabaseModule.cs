using AElf.Database;
using AElf.Database.Config;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class DatabaseModule : Module
    {
        private readonly IDatabaseConfig _config;

        public DatabaseModule(IDatabaseConfig config)
        {
            _config = config;
        }

        protected override void Load(ContainerBuilder builder)
        {
            switch (_config.Type)
            {
                case DatabaseType.KeyValue:
                    builder.RegisterType<KeyValueDatabase>().As<IKeyValueDatabase>().SingleInstance();
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