using AElf.Common.Serializers;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using Autofac;

namespace AElf.Kernel
{
    public class KernelAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ProtobufSerializer>().As<IByteSerializer>().SingleInstance();
            
            builder.RegisterType<StateStore>().As<IStateStore>().SingleInstance();
            builder.RegisterType<TransactionStore>().As<ITransactionStore>().SingleInstance();
            builder.RegisterType<BinaryMerkleTreeStore>().As<IBinaryMerkleTreeStore>().SingleInstance();
            builder.RegisterType<BlockHeaderStore>().As<IBlockHeaderStore>().SingleInstance();
            builder.RegisterType<BlockBodyStore>().As<IBlockBodyStore>().SingleInstance();
            builder.RegisterType<ChainHeightStore>().As<IChainHeightStore>().SingleInstance();
            builder.RegisterType<GenesisBlockHashStore>().As<IGenesisBlockHashStore>().SingleInstance();
            builder.RegisterType<CurrentBlockHashStore>().As<ICurrentBlockHashStore>().SingleInstance();
            builder.RegisterType<MinersStore>().As<IMinersStore>().SingleInstance();
            builder.RegisterType<SmartContractStore>().As<ISmartContractStore>().SingleInstance();
            builder.RegisterType<TransactionReceiptStore>().As<ITransactionReceiptStore>().SingleInstance();
            builder.RegisterType<TransactionResultStore>().As<ITransactionResultStore>().SingleInstance();
            builder.RegisterType<TransactionTraceStore>().As<ITransactionTraceStore>().SingleInstance();
            builder.RegisterType<CanonicalStore>().As<ICanonicalStore>().SingleInstance();
            builder.RegisterType<FunctionMetadataStore>().As<IFunctionMetadataStore>().SingleInstance();
            builder.RegisterType<CallGraphStore>().As<ICallGraphStore>().SingleInstance();
            
            builder.RegisterType<StateManager>().As<IStateManager>().SingleInstance();
            builder.RegisterType<TransactionManager>().As<ITransactionManager>().SingleInstance();
            builder.RegisterType<BinaryMerkleTreeManager>().As<IBinaryMerkleTreeManager>().SingleInstance();
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