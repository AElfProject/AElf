using Autofac;
using AElf.Configuration;
using AElf.Kernel.Node;
using AElf.Node.AElfChain;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class MainChainNodeModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AElf.Node.Node>().As<INode>();
            builder.RegisterType<P2PHandler>().PropertiesAutowired();

        }
    }
}