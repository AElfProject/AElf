using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    internal interface ITransactionSizeFeeSymbolsProvider
    {
        Task<TransactionSizeFeeSymbols> GetTransactionSizeFeeSymbolsAsync(IChainContext chainContext);
        Task SetTransactionSizeFeeSymbolsAsync(IBlockIndex blockIndex, TransactionSizeFeeSymbols transactionSizeFeeSymbols);
    }

    internal class TransactionSizeFeeSymbolsProvider : BlockExecutedDataBaseProvider<TransactionSizeFeeSymbols>, ITransactionSizeFeeSymbolsProvider,
        ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(TransactionSizeFeeSymbols);

        public TransactionSizeFeeSymbolsProvider(
            ICachedBlockchainExecutedDataService<TransactionSizeFeeSymbols> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {
        }

        public Task<TransactionSizeFeeSymbols> GetTransactionSizeFeeSymbolsAsync(IChainContext chainContext)
        {
            var transactionSizeFeeSymbols = GetBlockExecutedData(chainContext);
            return Task.FromResult(transactionSizeFeeSymbols);
        }

        public async Task SetTransactionSizeFeeSymbolsAsync(IBlockIndex blockIndex,
            TransactionSizeFeeSymbols transactionSizeFeeSymbols)
        {
            await AddBlockExecutedDataAsync(blockIndex, transactionSizeFeeSymbols);
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}