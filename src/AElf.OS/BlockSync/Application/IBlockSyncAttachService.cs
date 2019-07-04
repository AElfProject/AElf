using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network;
using AElf.OS.Network.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockSyncAttachService
    {
        void EnqueueAttachBlockWithTransactionsJob(BlockWithTransactions blockWithTransactions);
    }

    public class BlockSyncAttachService : IBlockSyncAttachService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockAttachService _blockAttachService;
        private readonly ITaskQueueManager _taskQueueManager;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;
        private readonly IBlockValidationService _validationService;
        
        public ILogger<BlockSyncAttachService> Logger { get; set; }

        public BlockSyncAttachService(IBlockchainService blockchainService,
            IBlockAttachService blockAttachService,
            ITaskQueueManager taskQueueManager,
            IBlockSyncStateProvider blockSyncStateProvider,
            IBlockValidationService validationService)
        {
            Logger = NullLogger<BlockSyncAttachService>.Instance;
            
            _blockAttachService = blockAttachService;
            _taskQueueManager = taskQueueManager;
            _blockSyncStateProvider = blockSyncStateProvider;
            _validationService = validationService;
            _blockchainService = blockchainService;
        }

        private async Task AttachBlockWithTransactionsAsync(BlockWithTransactions blockWithTransactions)
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

            var enqueueTimestamp = TimestampHelper.GetUtcNow();
            _taskQueueManager.Enqueue(async () =>
                {
                    try
                    {
                        _blockSyncStateProvider.BlockSyncJobEnqueueTime = enqueueTimestamp;
                        await _blockAttachService.AttachBlockAsync(block);
                    }
                    finally
                    {
                        _blockSyncStateProvider.BlockSyncJobEnqueueTime = null;
                    }
                },
                KernelConstants.UpdateChainQueueName);
        }

        public void EnqueueAttachBlockWithTransactionsJob(BlockWithTransactions blockWithTransactions)
        {
            Logger.LogTrace($"Receive block to sync {{ hash: {blockWithTransactions.GetHash()}, height: {blockWithTransactions.Header.Height} }} .");
            
            var enqueueTimestamp = TimestampHelper.GetUtcNow();
            _taskQueueManager.Enqueue(async () =>
                {
                    try
                    {
                        _blockSyncStateProvider.BlockSyncAttachBlockEnqueueTime = enqueueTimestamp;
                        await AttachBlockWithTransactionsAsync(blockWithTransactions);
                    }
                    finally
                    {
                        _blockSyncStateProvider.BlockSyncAttachBlockEnqueueTime = null;
                    }
                },
                OSConsts.BlockSyncAttachQueueName);
        }
    }
}