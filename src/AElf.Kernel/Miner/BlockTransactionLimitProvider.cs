using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Miner
{
    public interface IBlockTransactionLimitProvider
    {
        Task<int> GetLimitAsync(IChainContext chainContext);
        Task SetLimitAsync(IBlockIndex blockIndex, int limit);
    }

    internal class BlockTransactionLimitProvider : BlockExecutedDataBaseProvider<Int32Value>, IBlockTransactionLimitProvider,
        ISingletonDependency
    {
        private const string BlockExecutedDataName = "BlockTransactionLimit";

        public BlockTransactionLimitProvider(
            ICachedBlockchainExecutedDataService<Int32Value> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {
        }

        public Task<int> GetLimitAsync(IChainContext chainContext)
        {
            var limit = GetBlockExecutedData(chainContext);
            return Task.FromResult(limit?.Value ?? int.MaxValue);
        }

        public async Task SetLimitAsync(IBlockIndex blockIndex, int limit)
        {
            var blockTransactionLimit = new Int32Value
            {
                Value = limit
            };
            await AddBlockExecutedDataAsync(blockIndex, blockTransactionLimit);
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}