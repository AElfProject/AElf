using Autofac;
using AElf.SmartContract;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class MainModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            //TODO : REVIEW - probably not a good idea

            var assembly1 = typeof(IWorldStateDictator).Assembly;

            builder.RegisterInstance<IHash>(new Hash()).As<Hash>();

            builder.RegisterAssemblyTypes(assembly1).AsImplementedInterfaces();

            var assembly2 = typeof(ISerializer<>).Assembly;
            builder.RegisterAssemblyTypes(assembly2).AsImplementedInterfaces();

            var assembly3 = typeof(WorldStateDictator).Assembly;
            builder.RegisterAssemblyTypes(assembly3).AsImplementedInterfaces();

            
            builder.RegisterType(typeof(Hash)).As(typeof(IHash));

            builder.RegisterGeneric(typeof(Serializer<>)).As(typeof(ISerializer<>));

                        

            
            base.Load(builder);
        }
    }
}