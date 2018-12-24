using AElf.Common.Serializers;
using AElf.Kernel.Manager.Interfaces;
using AElf.Kernel.Manager.Managers;
using AElf.Kernel.Storage;
using Autofac;

namespace AElf.Kernel
{
    public class KernelAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ProtobufSerializer>().As<IByteSerializer>().SingleInstance();
            
            builder.RegisterType<StateStore>().SingleInstance();
            builder.RegisterType<TransactionStore>().SingleInstance();
            builder.RegisterType<MerkleTreeStore>().SingleInstance();
            builder.RegisterType<BlockHeaderStore>().SingleInstance();
            builder.RegisterType<BlockBodyStore>().SingleInstance();
            builder.RegisterType<ChainHeightStore>().SingleInstance();
            builder.RegisterType<GenesisBlockHashStore>().SingleInstance();
            builder.RegisterType<CurrentBlockHashStore>().SingleInstance();
            builder.RegisterType<MinersStore>().SingleInstance();
            builder.RegisterType<SmartContractStore>().SingleInstance();
            builder.RegisterType<TransactionReceiptStore>().SingleInstance();
            builder.RegisterType<TransactionResultStore>().SingleInstance();
            builder.RegisterType<TransactionTraceStore>().SingleInstance();
            builder.RegisterType<CanonicalStore>().SingleInstance();
            builder.RegisterType<FunctionMetadataStore>().SingleInstance();
            builder.RegisterType<CallGraphStore>().SingleInstance();
            
            builder.RegisterType<StateManager>().As<IStateManager>().SingleInstance();
            builder.RegisterType<TransactionManager>().As<ITransactionManager>().SingleInstance();
            builder.RegisterType<MerkleTreeManager>().As<IMerkleTreeManager>().SingleInstance();
            builder.RegisterType<BlockManager>().As<IBlockManager>().SingleInstance();
            builder.RegisterType<ChainManager>().As<IChainManager>().SingleInstance();
            builder.RegisterType<MinersManager>().As<IMinersManager>().SingleInstance();
            builder.RegisterType<SmartContractManager>().As<ISmartContractManager>().SingleInstance();
            builder.RegisterType<TransactionReceiptManager>().As<ITransactionReceiptManager>().SingleInstance();
            builder.RegisterType<TransactionResultManager>().As<ITransactionResultManager>().SingleInstance();
            builder.RegisterType<TransactionTraceManager>().As<ITransactionTraceManager>().SingleInstance();
            builder.RegisterType<FunctionFunctionMetadataManager>().As<IFunctionMetadataManager>().SingleInstance();
        }
    }
}