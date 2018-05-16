using System;
using AElf.Database;
using Autofac;
using ServiceStack.Redis;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class DatabaseModule : Module
    {
        private readonly bool _useRedis = false;
        protected override void Load(ContainerBuilder builder)
        {
            if (_useRedis)
            {
                if (!new RedisDatabase().IsConnected()) 
                    return;
                
                builder.RegisterType<RedisDatabase>().As<IKeyValueDatabase>();
            }
            else
            {
                builder.RegisterType<KeyValueDatabase>().As<IKeyValueDatabase>().SingleInstance();
            }
        }
    }
}