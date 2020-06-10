using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Txn.Application
{
    public interface ITransactionPackingOptionProvider
    {
        Task SetTransactionPackingOptionAsync(IBlockIndex blockIndex, bool isTransactionPackable);
        bool IsTransactionPackable(IChainContext chainContext);
    }

    public class TransactionPackingOptionProvider : BlockExecutedDataBaseProvider<BoolValue>,
        ITransactionPackingOptionProvider, ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(TransactionPackingOptionProvider);

        public TransactionPackingOptionProvider(
            ICachedBlockchainExecutedDataService<BoolValue> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }

        public async Task SetTransactionPackingOptionAsync(IBlockIndex blockIndex, bool isTransactionPackable)
        {
            await AddBlockExecutedDataAsync(blockIndex, new BoolValue {Value = isTransactionPackable});
        }

        public bool IsTransactionPackable(IChainContext chainContext)
        {
            return GetBlockExecutedData(chainContext)?.Value ?? true;
        }
    }
}