using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    public interface ITransactionSizeFeeSymbolsProvider
    {
        Task<TransactionSizeFeeSymbols> GetTransactionSizeFeeSymbolsAsync(IChainContext chainContext);
        Task SetTransactionSizeFeeSymbolsAsync(BlockIndex blockIndex, TransactionSizeFeeSymbols transactionSizeFeeSymbols);
    }

    public class TransactionSizeFeeSymbolsProvider : BlockExecutedCacheProvider, ITransactionSizeFeeSymbolsProvider,
        ISingletonDependency
    {
        private const string BlockExecutedDataName = "TransactionSizeFeeSymbols";

        private TransactionSizeFeeSymbols _transactionSizeFeeSymbols;

        private readonly IBlockchainStateService _blockchainStateService;

        public TransactionSizeFeeSymbolsProvider(IBlockchainStateService blockchainStateService)
        {
            _blockchainStateService = blockchainStateService;
        }
        
        public async Task<TransactionSizeFeeSymbols> GetTransactionSizeFeeSymbolsAsync(IChainContext chainContext)
        {
            if (_transactionSizeFeeSymbols == null)
            {
                _transactionSizeFeeSymbols = await GetSymbolsFromStateAsync(chainContext);
            }

            return _transactionSizeFeeSymbols;
        }

        private async Task<TransactionSizeFeeSymbols> GetSymbolsFromStateAsync(IChainContext chainContext)
        {
            var key = GetBlockExecutedCacheKey();
            return await _blockchainStateService.GetBlockExecutedDataAsync<TransactionSizeFeeSymbols>(chainContext, key);
        }

        public async Task SetTransactionSizeFeeSymbolsAsync(BlockIndex blockIndex, TransactionSizeFeeSymbols transactionSizeFeeSymbols)
        {
            var key = GetBlockExecutedCacheKey();
            await _blockchainStateService.AddBlockExecutedDataAsync(blockIndex.BlockHash, key, transactionSizeFeeSymbols);
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}