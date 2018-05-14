using System;
using AElf.Database;
using Autofac;
using ServiceStack.Redis;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class DatabaseModule : Module
    {
        private static readonly bool _useRedis = false;
        protected override void Load(ContainerBuilder builder)
        {
            if (_useRedis)
            {
                if (!new RedisDatabase().IsConnected()) 
                    return;
                
                builder.RegisterInstance(
                        new DatabaseConfig {IpAddress = "127.0.0.1", Port = 6379})
                    .As<DatabaseConfig>();
                builder.RegisterType<RedisDatabase>().As<IKeyValueDatabase>();
            }
            else
            {
                builder.RegisterType<KeyValueDatabase>().As<IKeyValueDatabase>();
            }
        }
    }
}