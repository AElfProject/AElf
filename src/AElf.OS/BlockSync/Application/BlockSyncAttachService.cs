using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.Network;
using AElf.OS.Network.Extensions;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncAttachService : IBlockSyncAttachService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockAttachService _blockAttachService;
        private readonly IBlockSyncQueueService _blockSyncQueueService;
        private readonly IBlockSyncValidationService _validationService;

        public ILogger<BlockSyncAttachService> Logger { get; set; }

        public BlockSyncAttachService(IBlockchainService blockchainService,
            IBlockAttachService blockAttachService,
            IBlockSyncValidationService validationService,
            IBlockSyncQueueService blockSyncQueueService)
        {
            Logger = NullLogger<BlockSyncAttachService>.Instance;

            _blockchainService = blockchainService;
            _blockAttachService = blockAttachService;
            _validationService = validationService;
            _blockSyncQueueService = blockSyncQueueService;
        }

        public async Task AttachBlockWithTransactionsAsync(BlockWithTransactions blockWithTransactions,
            Func<Task> attachFinishedCallback = null)
        {
            var txsValid = await _validationService.ValidateTransactionAsync(blockWithTransactions.Transactions);
            if (!txsValid)
            {
                throw new InvalidOperationException($"Tx in this block was invalid: {blockWithTransactions}");
            }

            var blockValid = await _validationService.ValidateBlockBeforeAttachAsync(blockWithTransactions);
            if (!blockValid)
            {
                throw new InvalidOperationException(
                    $"The block was invalid: {blockWithTransactions}.");
            }

            await _blockchainService.AddTransactionsAsync(blockWithTransactions.Transactions);
            var block = blockWithTransactions.ToBlock();
            await _blockchainService.AddBlockAsync(block);

            _blockSyncQueueService.Enqueue(async () =>
                {
                    try
                    {
                        await _blockAttachService.AttachBlockAsync(block);
                    }
                    finally
                    {
                        if (attachFinishedCallback != null)
                        {
                            await attachFinishedCallback();
                        }
                    }
                },
                KernelConstants.UpdateChainQueueName);
        }
    }
}