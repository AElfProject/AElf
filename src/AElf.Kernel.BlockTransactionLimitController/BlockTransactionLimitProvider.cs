using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.BlockTransactionLimitController
{
    public interface IBlockTransactionLimitProvider
    {
        Task<int> GetLimitAsync(IChainContext chainContext);
        Task SetLimitAsync(Hash blockHash, int limit);
    }

    public class BlockTransactionLimitProvider : BlockExecutedCacheProvider, IBlockTransactionLimitProvider,
        ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(BlockTransactionLimit);

        private readonly IBlockchainExecutedDataService _blockchainExecutedDataService;

        public BlockTransactionLimitProvider(IBlockchainExecutedDataService blockchainExecutedDataService)
        {
            _blockchainExecutedDataService = blockchainExecutedDataService;
        }


        public async Task<int> GetLimitAsync(IChainContext chainContext)
        {
            var key = GetBlockExecutedCacheKey();
            var limit =
                await _blockchainExecutedDataService.GetBlockExecutedDataAsync<BlockTransactionLimit>(chainContext, key);
            return limit?.Value ?? 0;
        }

        public async Task SetLimitAsync(Hash blockHash, int limit)
        {
            var key = GetBlockExecutedCacheKey();
            var blockTransactionLimit = new BlockTransactionLimit
            {
                Value = limit
            };
            await _blockchainExecutedDataService.AddBlockExecutedDataAsync(blockHash, key, blockTransactionLimit);
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}