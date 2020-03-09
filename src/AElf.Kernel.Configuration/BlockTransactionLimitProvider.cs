using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Configuration
{
    public interface IBlockTransactionLimitProvider
    {
        Task<int> GetLimitAsync(IChainContext chainContext);
        Task SetLimitAsync(Hash blockHash, int limit);
    }

    public class BlockTransactionLimitProvider : BlockExecutedDataProvider, IBlockTransactionLimitProvider,
        ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(BlockTransactionLimit);

        private readonly ICachedBlockchainExecutedDataService<BlockTransactionLimit>
            _cachedBlockchainExecutedDataService;

        public BlockTransactionLimitProvider(ICachedBlockchainExecutedDataService<BlockTransactionLimit>
                cachedBlockchainExecutedDataService)
        {
            _cachedBlockchainExecutedDataService = cachedBlockchainExecutedDataService;
        }


        public Task<int> GetLimitAsync(IChainContext chainContext)
        {
            var key = GetBlockExecutedDataKey();
            var limit = _cachedBlockchainExecutedDataService.GetBlockExecutedData(chainContext, key);
            return Task.FromResult(limit?.Value ?? 0);
        }

        public async Task SetLimitAsync(Hash blockHash, int limit)
        {
            var key = GetBlockExecutedDataKey();
            var blockTransactionLimit = new BlockTransactionLimit
            {
                Value = limit
            };
            await _cachedBlockchainExecutedDataService.AddBlockExecutedDataAsync(blockHash, key, blockTransactionLimit);
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}