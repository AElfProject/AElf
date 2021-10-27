using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    internal interface ITotalTransactionFeesMapProvider
    {
        Task<TotalTransactionFeesMap> GetTotalTransactionFeesMapAsync(IChainContext chainContext);
        Task SetTotalTransactionFeesMapAsync(IBlockIndex blockIndex, TotalTransactionFeesMap totalTransactionFeesMap);
    }

    internal class TotalTransactionFeesMapProvider : BlockExecutedDataBaseProvider<TotalTransactionFeesMap>,
        ITotalTransactionFeesMapProvider, ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(TotalTransactionFeesMap);

        public TotalTransactionFeesMapProvider(
            ICachedBlockchainExecutedDataService<TotalTransactionFeesMap> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {
        }

        public Task<TotalTransactionFeesMap> GetTotalTransactionFeesMapAsync(IChainContext chainContext)
        {
            var totalTxFeesMap = GetBlockExecutedData(chainContext);
            return Task.FromResult(totalTxFeesMap);
        }

        public async Task SetTotalTransactionFeesMapAsync(IBlockIndex blockIndex,
            TotalTransactionFeesMap totalTransactionFeesMap)
        {
            await AddBlockExecutedDataAsync(blockIndex, totalTransactionFeesMap);
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}