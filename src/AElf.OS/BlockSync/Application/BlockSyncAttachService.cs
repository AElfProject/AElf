using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.BlockSync.Events;
using AElf.OS.Network;
using AElf.OS.Network.Extensions;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncAttachService : IBlockSyncAttachService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockAttachService _blockAttachService;
        private readonly IBlockSyncQueueService _blockSyncQueueService;
        private readonly IBlockSyncValidationService _blockSyncValidationService;

        public ILocalEventBus LocalEventBus { get; set; }

        public ILogger<BlockSyncAttachService> Logger { get; set; }

        public BlockSyncAttachService(IBlockchainService blockchainService,
            IBlockAttachService blockAttachService,
            IBlockSyncValidationService blockSyncValidationService,
            IBlockSyncQueueService blockSyncQueueService)
        {
            Logger = NullLogger<BlockSyncAttachService>.Instance;
            LocalEventBus = NullLocalEventBus.Instance;

            _blockchainService = blockchainService;
            _blockAttachService = blockAttachService;
            _blockSyncValidationService = blockSyncValidationService;
            _blockSyncQueueService = blockSyncQueueService;
        }

        public async Task AttachBlockWithTransactionsAsync(BlockWithTransactions blockWithTransactions,
            string senderPubkey, Func<Task> attachFinishedCallback = null)
        {
            var blockValid = await _blockSyncValidationService.ValidateBlockBeforeAttachAsync(blockWithTransactions);
            if (!blockValid)
            {
                await LocalEventBus.PublishAsync(new BlockValidationFailedEventData
                {
                    BlockHash = blockWithTransactions.GetHash(),
                    BlockHeight = blockWithTransactions.Height,
                    BlockSenderPubkey = senderPubkey
                });
                
                return;
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