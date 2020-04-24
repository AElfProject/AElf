using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Configuration
{
    public interface IBlockTransactionLimitProvider
    {
        Task<int> GetLimitAsync(IChainContext chainContext);
        Task SetLimitAsync(IBlockIndex blockIndex, int limit);
    }

    public class BlockTransactionLimitProvider : BlockExecutedDataBaseProvider<BlockTransactionLimit>, IBlockTransactionLimitProvider,
        ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(BlockTransactionLimit);

        public BlockTransactionLimitProvider(
            ICachedBlockchainExecutedDataService<BlockTransactionLimit> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {
        }

        public Task<int> GetLimitAsync(IChainContext chainContext)
        {
            var limit = GetBlockExecutedData(chainContext);
            return Task.FromResult(limit?.Value ?? 0);
        }

        public async Task SetLimitAsync(IBlockIndex blockIndex, int limit)
        {
            var blockTransactionLimit = new BlockTransactionLimit
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