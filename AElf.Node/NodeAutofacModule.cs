using AElf.ChainController;
using AElf.Common.Enums;
using AElf.Configuration.Config.Consensus;
using AElf.Kernel.Node;
using AElf.Network;
using AElf.Node.AElfChain;
using AElf.Node.Protocol;
using AElf.Synchronization.BlockSynchronization;
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
            builder.RegisterType<MainchainNodeService>().As<INodeService>().SingleInstance();
            builder.RegisterType<NetworkManager>().As<INetworkManager>().SingleInstance();
            builder.RegisterType<BlockSet>().As<IBlockSet>().SingleInstance();

            if (ConsensusConfig.Instance.ConsensusType == ConsensusType.AElfDPoS)
            {
                builder.RegisterType<DPoS>().As<IConsensus>().SingleInstance();
            }
        }
    }
}