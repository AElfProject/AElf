using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;

namespace AElf.Kernel.Miner.Application
{
    public class BlockValidationProvider : IBlockValidationProvider
    {
        private readonly IBlockTransactionLimitProvider _blockTransactionLimitProvider;
        private readonly int _systemTransactionCount;

        public BlockValidationProvider(IBlockTransactionLimitProvider blockTransactionLimitProvider, 
            IEnumerable<ISystemTransactionGenerator> systemTransactionGenerators)
        {
            _blockTransactionLimitProvider = blockTransactionLimitProvider;
            _systemTransactionCount = systemTransactionGenerators.Count();
        }


        public Task<bool> ValidateBeforeAttachAsync(IBlock block)
        {
            return Task.FromResult(true);
        }

        public async Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            var txCountLimit = await _blockTransactionLimitProvider.GetLimitAsync(new ChainContext
            {
                BlockHash = block.Header.PreviousBlockHash,
                BlockHeight = block.Header.Height - 1
            });

            return Math.Max(txCountLimit, _systemTransactionCount) >= block.TransactionIds.Count();
        }

        public Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            return Task.FromResult(true);
        }
    }
}