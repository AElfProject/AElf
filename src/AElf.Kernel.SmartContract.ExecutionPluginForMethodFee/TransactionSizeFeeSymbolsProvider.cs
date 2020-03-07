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

    public class TransactionSizeFeeSymbolsProvider : BlockExecutedDataProvider, ITransactionSizeFeeSymbolsProvider,
        ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(TransactionSizeFeeSymbols);

        private TransactionSizeFeeSymbols _transactionSizeFeeSymbols;

        private readonly IBlockchainExecutedDataService _blockchainExecutedDataService;

        public TransactionSizeFeeSymbolsProvider(IBlockchainExecutedDataService blockchainExecutedDataService)
        {
            _blockchainExecutedDataService = blockchainExecutedDataService;
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
            var key = GetBlockExecutedDataKey();
            return await _blockchainExecutedDataService.GetBlockExecutedDataAsync<TransactionSizeFeeSymbols>(chainContext, key);
        }

        public async Task SetTransactionSizeFeeSymbolsAsync(BlockIndex blockIndex, TransactionSizeFeeSymbols transactionSizeFeeSymbols)
        {
            var key = GetBlockExecutedDataKey();
            await _blockchainExecutedDataService.AddBlockExecutedDataAsync(blockIndex.BlockHash, key, transactionSizeFeeSymbols);
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}