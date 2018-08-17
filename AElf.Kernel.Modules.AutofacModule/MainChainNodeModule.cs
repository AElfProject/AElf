using Autofac;
using AElf.Configuration;
using AElf.Kernel.Node;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class MainChainNodeModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MainChainNode>().As<IAElfNode>();
            builder.RegisterType<P2PHandler>().PropertiesAutowired();

        }
    }
}