using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class MainModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            //TODO : REVIEW - probably not a good idea
            
            var assembly = typeof(IAccount).Assembly;
            
            builder.RegisterInstance<IHash>(new Hash()).As<Hash>();
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();
            builder.RegisterGeneric(typeof(Serializer<>)).As(typeof(ISerializer<>));
            
            base.Load(builder);
        }
    }
}