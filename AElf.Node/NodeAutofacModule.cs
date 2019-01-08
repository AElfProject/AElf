using AElf.Common.Enums;
using AElf.Common.MultiIndexDictionary;
using AElf.Configuration.Config.Consensus;
using AElf.Kernel.Consensus;
using AElf.Network;
using AElf.Node.AElfChain;
using AElf.Node.Consensus;
using AElf.Node.Protocol;
using Autofac;

namespace AElf.Node
{
    // TODO: confuse AElf.Kernel.Consensus and AElf.Node.Consensus, maybe refactor namespace later
    public class NodeAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var assembly = typeof(Node).Assembly;
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();
            builder.RegisterType<Node>().As<INode>();
            builder.RegisterType<MainchainNodeService>().As<INodeService>().SingleInstance();
            builder.RegisterType<NetworkManager>().As<INetworkManager>().SingleInstance();
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
                    break;
                case ConsensusType.SingleNode:
                    break;
            }
        }
    }
}