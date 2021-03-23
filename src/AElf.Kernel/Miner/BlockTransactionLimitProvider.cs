using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Miner
{
    public interface IBlockTransactionLimitProvider
    {
        Task<int> GetLimitAsync(IBlockIndex blockIndex);
        Task SetLimitAsync(IBlockIndex blockIndex, int limit);
    }

    internal class BlockTransactionLimitProvider : BlockExecutedDataBaseProvider<Int32Value>, IBlockTransactionLimitProvider,
        ISingletonDependency
    {
        private readonly BlockTransactionLimitOptions _txLimitOptions;
        private const string BlockExecutedDataName = "BlockTransactionLimit";
        private readonly int _systemTransactionCount;

        public ILogger<BlockTransactionLimitProvider> Logger { get; set; }
        
        public BlockTransactionLimitProvider(
            ICachedBlockchainExecutedDataService<Int32Value> cachedBlockchainExecutedDataService, 
            IEnumerable<ISystemTransactionGenerator> systemTransactionGenerators,
            IOptionsSnapshot<BlockTransactionLimitOptions> txLimitOptions) : base(
            cachedBlockchainExecutedDataService)
        {
            _txLimitOptions = txLimitOptions.Value;
            _systemTransactionCount = systemTransactionGenerators.Count();
        }

        public Task<int> GetLimitAsync(IBlockIndex blockIndex)
        {
            return Task.FromResult(_txLimitOptions.TransactionLimit);
        }

        public async Task SetLimitAsync(IBlockIndex blockIndex, int limit)
        {
            if (limit <= _systemTransactionCount)
                return;
            
            var blockTransactionLimit = new Int32Value
            {
                Value = limit
            };
            await AddBlockExecutedDataAsync(blockIndex, blockTransactionLimit);
            Logger.LogDebug($"BlockTransactionLimit has been changed to {limit}");
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}