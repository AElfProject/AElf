using AElf.Common.Serializers;
using AElf.Kernel.Manager.Interfaces;
using AElf.Kernel.Manager.Managers;
using AElf.Kernel.Storage.Interfaces;
using AElf.Kernel.Storage.Storages;
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
            builder.RegisterType<MerkleTreeStore>().As<IMerkleTreeStore>().SingleInstance();
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
            
            builder.RegisterType<StateManager>().As<IStateManager>();
            builder.RegisterType<TransactionManager>().As<ITransactionManager>();
            builder.RegisterType<MerkleTreeManager>().As<IMerkleTreeManager>();
            builder.RegisterType<BlockManager>().As<IBlockManager>();
            builder.RegisterType<ChainManager>().As<IChainManager>();
            builder.RegisterType<MinersManager>().As<IMinersManager>();
            builder.RegisterType<SmartContractManager>().As<ISmartContractManager>();
            builder.RegisterType<TransactionReceiptManager>().As<ITransactionReceiptManager>();
            builder.RegisterType<TransactionResultManager>().As<ITransactionResultManager>();
            builder.RegisterType<TransactionTraceManager>().As<ITransactionTraceManager>();
            builder.RegisterType<FunctionFunctionMetadataManager>().As<IFunctionMetadataManager>();
        }
    }
}