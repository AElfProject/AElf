using AElf.Kernel.Concurrency.Metadata;
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
            
            builder.RegisterAssemblyTypes(assembly).Where(r => r.Name != typeof(FunctionMetadataService).Name).AsImplementedInterfaces();

            builder.RegisterType(typeof(Hash)).As(typeof(IHash));

            builder.RegisterGeneric(typeof(Serializer<>)).As(typeof(ISerializer<>));

            base.Load(builder);
        }
    }
}