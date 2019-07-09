using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.BlockSync.Infrastructure;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncQueueService : IBlockSyncQueueService
    {
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;
        private readonly ITaskQueueManager _taskQueueManager;
        
        public ILogger<BlockSyncQueueService> Logger { get; set; }

        public BlockSyncQueueService(IBlockSyncStateProvider blockSyncStateProvider, ITaskQueueManager taskQueueManager)
        {
            Logger = NullLogger<BlockSyncQueueService>.Instance;
            
            _blockSyncStateProvider = blockSyncStateProvider;
            _taskQueueManager = taskQueueManager;
        }

        public bool IsQueueAvailable(string queueName)
        {
            bool isAvailable;
            switch (queueName)
            {
                case OSConstants.BlockFetchQueueName:
                    isAvailable = CheckAgeLimit(_blockSyncStateProvider.BlockSyncFetchBlockEnqueueTime,
                        BlockSyncConstants.BlockSyncFetchBlockAgeLimit);
                    break;
                case OSConstants.BlockDownloadQueueName:
                    isAvailable = CheckAgeLimit(_blockSyncStateProvider.BlockSyncDownloadBlockEnqueueTime,
                        BlockSyncConstants.BlockSyncDownloadBlockAgeLimit);
                    break;
                case OSConstants.BlockSyncAttachQueueName:
                    isAvailable = CheckAgeLimit(_blockSyncStateProvider.BlockSyncAttachBlockEnqueueTime,
                        BlockSyncConstants.BlockSyncAttachBlockAgeLimit);
                    break;
                case KernelConstants.UpdateChainQueueName:
                    isAvailable = CheckAgeLimit(_blockSyncStateProvider.BlockSyncAttachAndExecuteBlockJobEnqueueTime,
                        BlockSyncConstants.BlockSyncAttachAndExecuteBlockAgeLimit);
                    break;
                default:
                    throw new InvalidOperationException($"invalid queue name: {queueName}");
            }

            return isAvailable;
        }

        public void Enqueue(Func<Task> task, string queueName)
        {
            var enqueueTime = TimestampHelper.GetUtcNow();
            _taskQueueManager.Enqueue(async () =>
            {
                try
                {
                    Logger.LogTrace($"Execute block sync job: {queueName}, enqueue time: {enqueueTime}");
                    SetEnqueueTimeByQueueName(queueName, enqueueTime);
                    await task();
                }
                finally
                {
                    SetEnqueueTimeByQueueName(queueName, null);
                }
            }, queueName);
        }

        private void SetEnqueueTimeByQueueName(string queueName, Timestamp enqueueTime)
        {
            switch (queueName)
            {
                case OSConstants.BlockFetchQueueName:
                    _blockSyncStateProvider.BlockSyncFetchBlockEnqueueTime = enqueueTime;
                    break;
                case OSConstants.BlockDownloadQueueName:
                    _blockSyncStateProvider.BlockSyncDownloadBlockEnqueueTime = enqueueTime;
                    break;
                case OSConstants.BlockSyncAttachQueueName:
                    _blockSyncStateProvider.BlockSyncAttachBlockEnqueueTime = enqueueTime;
                    break;
                case KernelConstants.UpdateChainQueueName:
                    _blockSyncStateProvider.BlockSyncAttachAndExecuteBlockJobEnqueueTime = enqueueTime;
                    break;
                default:
                    throw new InvalidOperationException($"invalid queue name: {queueName}");
            }
        }

        private bool CheckAgeLimit(Timestamp enqueueTime, long ageLimit)
        {
            if (enqueueTime != null && TimestampHelper.GetUtcNow() >
                enqueueTime + TimestampHelper.DurationFromMilliseconds(ageLimit))
            {
                Logger.LogDebug($"Enqueue time is more then limit : {enqueueTime}");
                return false;
            }

            return true;
        }
    }
}