using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IStateSizeLimitProvider
    {
        Task<int> GetStateSizeLimitAsync(IBlockIndex blockIndex);
        Task SetStateSizeLimitAsync(IBlockIndex blockIndex, int stateSizeLimit);
    }

    class StateSizeLimitProvider : BlockExecutedDataBaseProvider<Int32Value>, IStateSizeLimitProvider,
        ITransientDependency
    {
        private const string BlockExecutedDataName = "StateSizeLimit";
        public ILogger<StateSizeLimitProvider> Logger { get; set; }

        public Task<int> GetStateSizeLimitAsync(IBlockIndex blockIndex)
        {
            var stateSizeLimit = GetBlockExecutedData(blockIndex)?.Value ?? SmartContractConstants.StateSizeLimit;
            return Task.FromResult(stateSizeLimit);
        }

        public async Task SetStateSizeLimitAsync(IBlockIndex blockIndex, int stateSizeLimit)
        {
            if (stateSizeLimit <= 0)
                return;
            await AddBlockExecutedDataAsync(blockIndex, new Int32Value {Value = stateSizeLimit});
            Logger.LogDebug($"State limit size changed to {stateSizeLimit}");
        }

        public StateSizeLimitProvider(
            ICachedBlockchainExecutedDataService<Int32Value> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}