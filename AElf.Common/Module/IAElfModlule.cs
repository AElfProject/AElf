using Autofac;

namespace AElf.Common.Module
{
    public interface IAElfModlule
    {
        void Init(ContainerBuilder builder);
        void Run(IContainer container);
    }
}