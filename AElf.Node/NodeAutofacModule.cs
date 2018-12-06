using AElf.Common.Enums;
using AElf.Common.MultiIndexDictionary;
using AElf.Configuration.Config.Consensus;
using AElf.Kernel.Consensus;
using AElf.Kernel.Node;
using AElf.Network;
using AElf.Node.AElfChain;
using AElf.Node.Protocol;
using AElf.Synchronization.BlockSynchronization;
using Autofac;
using IConsensus = AElf.Kernel.Node.IConsensus;

namespace AElf.Node
{
    public class NodeAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var assembly = typeof(Node).Assembly;
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();
            builder.RegisterType<Node>().As<INode>();
            builder.RegisterType<MainchainNodeService>().As<INodeService>().SingleInstance();
            builder.RegisterType<NetworkManager>().As<INetworkManager>().SingleInstance();
            builder.RegisterType<BlockSet>().As<IBlockSet>().SingleInstance();
            builder.RegisterGeneric(typeof(EqualityIndex<,>)).As(typeof(IEqualityIndex<>));
            builder.RegisterGeneric(typeof(ComparisionIndex<,>)).As(typeof(IComparisionIndex<>));
            builder.RegisterType<ConsensusDataReader>();

            switch (ConsensusConfig.Instance.ConsensusType)
            {
                case ConsensusType.AElfDPoS:
                    builder.RegisterType<DPoS>().As<IConsensus>().SingleInstance();
                    builder.RegisterType<ConsensusHelper>();
                    break;
                case ConsensusType.PoW:
                    builder.RegisterType<PoW>().As<IConsensus>().SingleInstance();
                    break;
                case ConsensusType.SingleNode:
                    builder.RegisterType<StandaloneNodeConsensusPlaceHolder>().As<IConsensus>().SingleInstance();
                    break;
            }
        }
    }
}