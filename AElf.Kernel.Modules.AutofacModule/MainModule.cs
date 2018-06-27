using AElf.Kernel.Managers;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class MainModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            //TODO : REVIEW - probably not a good idea
            
            var assembly = typeof(IWorldStateDictator).Assembly;
            
            builder.RegisterInstance<IHash>(new Hash()).As<Hash>();
            
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();

            builder.RegisterType(typeof(Hash)).As(typeof(IHash));

            builder.RegisterGeneric(typeof(Serializer<>)).As(typeof(ISerializer<>));

            base.Load(builder);
        }
    }
}