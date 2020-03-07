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

    public class BlockTransactionLimitProvider : BlockExecutedDataProvider, IBlockTransactionLimitProvider,
        ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(BlockTransactionLimit);

        private readonly IBlockchainStateService _blockchainStateService;

        public BlockTransactionLimitProvider(IBlockchainStateService blockchainStateService)
        {
            _blockchainStateService = blockchainStateService;
        }

        public async Task<int> GetLimitAsync(IChainContext chainContext)
        {
            var key = GetBlockExecutedDataKey();
            var limit =
                await _blockchainStateService.GetBlockExecutedDataAsync<BlockTransactionLimit>(chainContext, key);
            return limit?.Value ?? 0;
        }

        public async Task SetLimitAsync(Hash blockHash, int limit)
        {
            var key = GetBlockExecutedDataKey();
            var blockTransactionLimit = new BlockTransactionLimit
            {
                Value = limit
            };
            await _blockchainStateService.AddBlockExecutedDataAsync(blockHash, key, blockTransactionLimit);
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}