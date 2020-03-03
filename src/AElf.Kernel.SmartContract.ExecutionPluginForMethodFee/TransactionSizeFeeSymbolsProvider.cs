using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    public interface ITransactionSizeFeeSymbolsProvider
    {
        Task<TransactionSizeFeeSymbols> GetTransactionSizeFeeSymbolsAsync(IChainContext chainContext);
        Task SetTransactionSizeFeeSymbolsAsync(Hash blockHash, TransactionSizeFeeSymbols transactionSizeFeeSymbols);
    }

    public class TransactionSizeFeeSymbolsProvider : BlockExecutedCacheProvider, ITransactionSizeFeeSymbolsProvider,
        ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(TransactionSizeFeeSymbols);

        private readonly IBlockchainStateService _blockchainStateService;

        public TransactionSizeFeeSymbolsProvider(IBlockchainStateService blockchainStateService)
        {
            _blockchainStateService = blockchainStateService;
        }
        
        public async Task<TransactionSizeFeeSymbols> GetTransactionSizeFeeSymbolsAsync(IChainContext chainContext)
        {
            var key = GetBlockExecutedCacheKey();
            return await _blockchainStateService.GetBlockExecutedDataAsync<TransactionSizeFeeSymbols>(chainContext, key);
        }

        public async Task SetTransactionSizeFeeSymbolsAsync(Hash blockHash, TransactionSizeFeeSymbols transactionSizeFeeSymbols)
        {
            var key = GetBlockExecutedCacheKey();
            await _blockchainStateService.AddBlockExecutedDataAsync(blockHash, key, transactionSizeFeeSymbols);
        }
        
        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}