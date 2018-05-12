using AElf.Database;
using AElf.Kernel.Storages;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class Module: Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var assembly = typeof(AElf.Kernel.IAccount).Assembly;
            
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();

            var client = RedisHelper.GetRedisClient();
            if (client.HasConnected)
            {
                builder.RegisterType(typeof(RedisDatabase)).As(typeof(IKeyValueDatabase));
            }
            else
            {
                builder.RegisterType(typeof(KeyValueDatabase)).As(typeof(IKeyValueDatabase));
            }

            builder.RegisterType(typeof(Hash)).As(typeof(IHash));
            
            builder.RegisterGeneric(typeof(Serializer<>)).As(typeof(ISerializer<>));

            base.Load(builder);
        }
    }
}