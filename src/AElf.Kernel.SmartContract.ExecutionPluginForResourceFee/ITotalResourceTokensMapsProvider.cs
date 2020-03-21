using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    public interface ITotalResourceTokensMapsProvider
    {
        Task<TotalResourceTokensMaps> GetTotalResourceTokensMapsAsync(IChainContext chainContext);
        Task SetTotalResourceTokensMapsAsync(IBlockIndex blockIndex, TotalResourceTokensMaps totalResourceTokensMap);
    }

    public class TotalResourceTokensMapsProvider : BlockExecutedDataBaseProvider<TotalResourceTokensMaps>,
        ITotalResourceTokensMapsProvider, ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(TotalResourceTokensMaps);

        public TotalResourceTokensMapsProvider(
            ICachedBlockchainExecutedDataService<TotalResourceTokensMaps> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {
        }

        public Task<TotalResourceTokensMaps> GetTotalResourceTokensMapsAsync(IChainContext chainContext)
        {
            var totalTxFeesMap = GetBlockExecutedData(chainContext);
            return Task.FromResult(totalTxFeesMap);
        }

        public async Task SetTotalResourceTokensMapsAsync(IBlockIndex blockIndex,
            TotalResourceTokensMaps totalResourceTokensMaps)
        {
            await AddBlockExecutedDataAsync(blockIndex, totalResourceTokensMaps);
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}