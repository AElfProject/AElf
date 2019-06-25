using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncBlockService : IBlockSyncBlockService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockValidationService _validationService;

        public BlockSyncBlockService(IBlockchainService blockchainService,
            IBlockValidationService validationService)
        {
            _blockchainService = blockchainService;
            _validationService = validationService;
        }

        public async Task AddBlockWithTransactionsAsync(Block block, IEnumerable<Transaction> transactions)
        {
            var valid = await _validationService.ValidateBlockBeforeAttachAsync(block);
            if (!valid)
            {
                throw new InvalidOperationException(
                    $"The block was invalid, block hash: {block}.");
            }

            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
        }
    }
}