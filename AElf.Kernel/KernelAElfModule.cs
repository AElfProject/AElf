using AElf.Common.Module;
using Autofac;

namespace AElf.Kernel
{
    public class KernelAElfModule:IAElfModlule
    {
        public void Init(ContainerBuilder builder)
        {
            builder.RegisterInstance<IHash>(new Hash()).As<Hash>();
            
            var assembly1 = typeof(ISerializer<>).Assembly;
            builder.RegisterAssemblyTypes(assembly1).AsImplementedInterfaces();
            
            var assembly2 = typeof(BlockHeader).Assembly;
            builder.RegisterAssemblyTypes(assembly2).AsImplementedInterfaces();

            builder.RegisterType(typeof(Hash)).As(typeof(IHash));

            builder.RegisterGeneric(typeof(Serializer<>)).As(typeof(ISerializer<>));
        }

        public void Run(ILifetimeScope scope)
        {
        }
    }
}