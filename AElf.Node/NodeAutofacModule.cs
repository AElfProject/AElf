using AElf.Kernel.Node;
using AElf.Network;
using AElf.Node.AElfChain;
using AElf.Node.Protocol;
using Autofac;

namespace AElf.Node
{
    public class NodeAutofacModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {            
            var assembly = typeof(Node).Assembly;
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();
            builder.RegisterType<Node>().As<INode>();
            builder.RegisterType<P2PHandler>().PropertiesAutowired();
            builder.RegisterType<MainchainNodeService>().As<INodeService>();
            builder.RegisterType<NetworkManager>().As<INetworkManager>().SingleInstance();
        }
    }
}