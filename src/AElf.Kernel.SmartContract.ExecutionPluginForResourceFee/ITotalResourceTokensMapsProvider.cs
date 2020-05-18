using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

        public ILogger<TotalResourceTokensMapsProvider> Logger { get; set; }

        public TotalResourceTokensMapsProvider(
            ICachedBlockchainExecutedDataService<TotalResourceTokensMaps> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {
            Logger = NullLogger<TotalResourceTokensMapsProvider>.Instance;
        }

        public Task<TotalResourceTokensMaps> GetTotalResourceTokensMapsAsync(IChainContext chainContext)
        {
            var totalTxFeesMap = GetBlockExecutedData(chainContext);
            Logger.LogTrace($"Get TotalResourceTokensMaps: {totalTxFeesMap}");
            return Task.FromResult(totalTxFeesMap);
        }

        public async Task SetTotalResourceTokensMapsAsync(IBlockIndex blockIndex,
            TotalResourceTokensMaps totalResourceTokensMaps)
        {
            Logger.LogTrace($"Add TotalResourceTokensMaps: {totalResourceTokensMaps}");
            await AddBlockExecutedDataAsync(blockIndex, totalResourceTokensMaps);
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}