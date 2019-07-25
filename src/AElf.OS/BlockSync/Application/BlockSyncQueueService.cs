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

        public bool ValidateQueueAvailability(string queueName)
        {
            bool isAvailable;
            var enqueueTime = _blockSyncStateProvider.GetEnqueueTime(queueName);
            switch (queueName)
            {
                case OSConstants.BlockFetchQueueName:
                    isAvailable = CheckAgeLimit(enqueueTime, BlockSyncConstants.BlockSyncFetchBlockAgeLimit);
                    break;
                case OSConstants.BlockSyncAttachQueueName:
                    isAvailable = CheckAgeLimit(enqueueTime, BlockSyncConstants.BlockSyncAttachBlockAgeLimit);
                    break;
                case KernelConstants.UpdateChainQueueName:
                    isAvailable = CheckAgeLimit(enqueueTime, BlockSyncConstants.BlockSyncAttachAndExecuteBlockAgeLimit);
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

                    _blockSyncStateProvider.SetEnqueueTime(queueName, enqueueTime);
                    await task();
                }
                finally
                {
                    _blockSyncStateProvider.SetEnqueueTime(queueName, null);
                }
            }, queueName);
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