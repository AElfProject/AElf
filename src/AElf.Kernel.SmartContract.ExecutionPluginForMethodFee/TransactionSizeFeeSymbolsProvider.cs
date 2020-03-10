using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    public interface ITransactionSizeFeeSymbolsProvider
    {
        Task<TransactionSizeFeeSymbols> GetTransactionSizeFeeSymbolsAsync(IChainContext chainContext);
        Task SetTransactionSizeFeeSymbolsAsync(BlockIndex blockIndex, TransactionSizeFeeSymbols transactionSizeFeeSymbols);
    }

    public class TransactionSizeFeeSymbolsProvider : BlockExecutedDataProvider, ITransactionSizeFeeSymbolsProvider,
        ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(TransactionSizeFeeSymbols);

        private readonly ICachedBlockchainExecutedDataService<TransactionSizeFeeSymbols> _cachedBlockchainExecutedDataService;

        public TransactionSizeFeeSymbolsProvider(ICachedBlockchainExecutedDataService<TransactionSizeFeeSymbols> cachedBlockchainExecutedDataService)
        {
            _cachedBlockchainExecutedDataService = cachedBlockchainExecutedDataService;
        }

        public Task<TransactionSizeFeeSymbols> GetTransactionSizeFeeSymbolsAsync(IChainContext chainContext)
        {
            var key = GetBlockExecutedDataKey();
            var transactionSizeFeeSymbols =
                _cachedBlockchainExecutedDataService.GetBlockExecutedData(chainContext, key);
            return Task.FromResult(transactionSizeFeeSymbols);
        }

        public async Task SetTransactionSizeFeeSymbolsAsync(BlockIndex blockIndex, TransactionSizeFeeSymbols transactionSizeFeeSymbols)
        {
            var key = GetBlockExecutedDataKey();
            await _cachedBlockchainExecutedDataService.AddBlockExecutedDataAsync(blockIndex.BlockHash, key,
                transactionSizeFeeSymbols);
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}