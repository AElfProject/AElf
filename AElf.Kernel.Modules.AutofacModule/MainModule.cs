using AElf.Database;
using AElf.Kernel.Storages;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class MainModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            //TODO : REVIEW - probably not a good idea
            
            var assembly = typeof(IAccount).Assembly;
            
            builder.RegisterInstance<IHash>(new Hash()).As<Hash>();
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