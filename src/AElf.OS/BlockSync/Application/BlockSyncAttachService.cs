using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.Network;
using AElf.OS.Network.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncAttachService : IBlockSyncAttachService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockAttachService _blockAttachService;
        private readonly IBlockSyncQueueService _blockSyncQueueService;
        private readonly IBlockValidationService _validationService;
        
        public ILogger<BlockSyncAttachService> Logger { get; set; }

        public BlockSyncAttachService(IBlockchainService blockchainService,
            IBlockAttachService blockAttachService,
            IBlockValidationService validationService,
            IBlockSyncQueueService blockSyncQueueService)
        {
            Logger = NullLogger<BlockSyncAttachService>.Instance;
            
            _blockchainService = blockchainService;
            _blockAttachService = blockAttachService;
            _validationService = validationService;
            _blockSyncQueueService = blockSyncQueueService;
        }

        public async Task AttachBlockWithTransactionsAsync(BlockWithTransactions blockWithTransactions)
        {
            var valid = await _validationService.ValidateBlockBeforeAttachAsync(blockWithTransactions);
            if (!valid)
            {
                throw new InvalidOperationException(
                    $"The block was invalid, block hash: {blockWithTransactions}.");
            }

            await _blockchainService.AddTransactionsAsync(blockWithTransactions.Transactions);
            var block = blockWithTransactions.ToBlock();
            await _blockchainService.AddBlockAsync(block);

            _blockSyncQueueService.Enqueue(async () => { await _blockAttachService.AttachBlockAsync(block); },
                KernelConstants.UpdateChainQueueName);
        }
    }
}