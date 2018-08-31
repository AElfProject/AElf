using Autofac;

namespace AElf.Common.Module
{
    public interface IAElfModule
    {
        void Init(ContainerBuilder builder);
        void Run(ILifetimeScope scope);
    }
}