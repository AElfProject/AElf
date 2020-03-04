using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    public interface ITransactionSizeFeeSymbolsProvider
    {
        Task<TransactionSizeFeeSymbols> GetTransactionSizeFeeSymbolsAsync(IChainContext chainContext);
        Task SetTransactionSizeFeeSymbolsAsync(BlockIndex blockIndex, TransactionSizeFeeSymbols transactionSizeFeeSymbols);
        Task ClearChangeHeightAsync(BlockIndex blockIndex);
    }

    public class TransactionSizeFeeSymbolsProvider : BlockExecutedCacheProvider, ITransactionSizeFeeSymbolsProvider,
        ISingletonDependency
    {
        private const string BlockExecutedDataName = "TransactionSizeFeeSymbols";

        private TransactionSizeFeeSymbols _transactionSizeFeeSymbols;
        private long? _changeHeight;

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
            else if (_changeHeight.HasValue)
            {
                return await GetSymbolsFromStateAsync(chainContext);
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
            if (_changeHeight == null || _changeHeight < blockIndex.BlockHeight) _changeHeight = blockIndex.BlockHeight;
        }
        
        public async Task ClearChangeHeightAsync(BlockIndex blockIndex)
        {
            if (_changeHeight == null || _changeHeight > blockIndex.BlockHeight) return;
            _transactionSizeFeeSymbols = await GetSymbolsFromStateAsync(new ChainContext
            {
                BlockHash = blockIndex.BlockHash,
                BlockHeight = blockIndex.BlockHeight
            });
            _changeHeight = null;
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}