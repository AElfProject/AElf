using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    public interface ITotalResourceTokensMapProvider
    {
        Task<TotalResourceTokensMap> GetTotalResourceTokensMapAsync(IChainContext chainContext);
        Task SetTotalResourceTokensMapAsync(IBlockIndex blockIndex, TotalResourceTokensMap totalResourceTokensMap);
    }

    public class TotalResourceTokensMapProvider : BlockExecutedDataBaseProvider<TotalResourceTokensMap>,
        ITotalResourceTokensMapProvider, ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(TotalResourceTokensMap);

        public TotalResourceTokensMapProvider(
            ICachedBlockchainExecutedDataService<TotalResourceTokensMap> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {
        }

        public Task<TotalResourceTokensMap> GetTotalResourceTokensMapAsync(IChainContext chainContext)
        {
            var totalTxFeesMap = GetBlockExecutedData(chainContext);
            return Task.FromResult(totalTxFeesMap);
        }

        public async Task SetTotalResourceTokensMapAsync(IBlockIndex blockIndex,
            TotalResourceTokensMap totalResourceTokensMap)
        {
            await AddBlockExecutedDataAsync(blockIndex, totalResourceTokensMap);
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}