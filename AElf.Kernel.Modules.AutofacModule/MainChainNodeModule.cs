using AElf.Kernel.Node;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class MainChainNodeModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MainChainNode>().As<IAElfNode>();
        }
    }
}