using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network;
using AElf.OS.Network.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncAttachService : IBlockSyncAttachService
    {
        private readonly IBlockAttachService _blockAttachService;
        private readonly ITaskQueueManager _taskQueueManager;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;
        private readonly IBlockSyncBlockService _blockSyncBlockService;
        
        public ILogger<BlockSyncAttachService> Logger { get; set; }

        public BlockSyncAttachService(IBlockAttachService blockAttachService,
            ITaskQueueManager taskQueueManager,
            IBlockSyncStateProvider blockSyncStateProvider,
            IBlockSyncBlockService blockSyncBlockService)
        {
            Logger = NullLogger<BlockSyncAttachService>.Instance;
            
            _blockAttachService = blockAttachService;
            _taskQueueManager = taskQueueManager;
            _blockSyncStateProvider = blockSyncStateProvider;
            _blockSyncBlockService = blockSyncBlockService;
        }

        private async Task AttachBlockWithTransactionsAsync(BlockWithTransactions blockWithTransactions)
        {
            var block = blockWithTransactions.ToBlock();
            await _blockSyncBlockService.AddBlockWithTransactionsAsync(block, blockWithTransactions.Transactions);

            var enqueueTimestamp = TimestampHelper.GetUtcNow();
            _taskQueueManager.Enqueue(async () =>
                {
                    try
                    {
                        _blockSyncStateProvider.BlockSyncAttachAndExecuteBlockJobEnqueueTime = enqueueTimestamp;
                        Logger.LogTrace(
                            $"Block sync: Attach and execute block, block height: {blockWithTransactions.Height}, block hash: {blockWithTransactions.GetHash()}, enqueue time: {enqueueTimestamp}");
                            
                        await _blockAttachService.AttachBlockAsync(block);
                    }
                    finally
                    {
                        _blockSyncStateProvider.BlockSyncAttachAndExecuteBlockJobEnqueueTime = null;
                    }
                },
                KernelConstants.UpdateChainQueueName);
        }

        public void EnqueueAttachBlockWithTransactionsJob(BlockWithTransactions blockWithTransactions)
        {
            Logger.LogTrace($"Receive announcement and sync block {{ hash: {blockWithTransactions.GetHash()}, height: {blockWithTransactions.Header.Height} }} .");
            
            var enqueueTimestamp = TimestampHelper.GetUtcNow();
            _taskQueueManager.Enqueue(async () =>
                {
                    try
                    {
                        _blockSyncStateProvider.BlockSyncAttachBlockEnqueueTime = enqueueTimestamp;
                        Logger.LogTrace(
                            $"Block sync: Attach block, block height: {blockWithTransactions.Height}, block hash: {blockWithTransactions.GetHash()}, enqueue time: {enqueueTimestamp}");
                        
                        await AttachBlockWithTransactionsAsync(blockWithTransactions);
                    }
                    finally
                    {
                        _blockSyncStateProvider.BlockSyncAttachBlockEnqueueTime = null;
                    }
                },
                OSConstants.BlockSyncAttachQueueName);
        }
    }
}