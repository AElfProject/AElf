using System;
using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.OS.BlockSync.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncQueueServiceTests : BlockSyncTestBase
    {
        private readonly IBlockSyncQueueService _blockSyncQueueService;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;

        public BlockSyncQueueServiceTests()
        {
            _blockSyncQueueService = GetRequiredService<IBlockSyncQueueService>();
            _blockSyncStateProvider = GetRequiredService<IBlockSyncStateProvider>();
        }

        [Fact]
        public void ValidateQueueAvailability_Test()
        {
            {
                var result = _blockSyncQueueService.ValidateQueueAvailability(OSConstants.BlockFetchQueueName);
                result.ShouldBeTrue();

                _blockSyncStateProvider.SetEnqueueTime(OSConstants.BlockFetchQueueName,
                    TimestampHelper.GetUtcNow());
                result = _blockSyncQueueService.ValidateQueueAvailability(OSConstants.BlockFetchQueueName);
                result.ShouldBeTrue();

                _blockSyncStateProvider.SetEnqueueTime(OSConstants.BlockFetchQueueName,
                    TimestampHelper.GetUtcNow()
                        .AddMilliseconds(-BlockSyncConstants.BlockSyncFetchBlockAgeLimit - 1000));
                result = _blockSyncQueueService.ValidateQueueAvailability(OSConstants.BlockFetchQueueName);
                result.ShouldBeFalse();
            }

            {
                var result = _blockSyncQueueService.ValidateQueueAvailability(OSConstants.BlockSyncAttachQueueName);
                result.ShouldBeTrue();

                _blockSyncStateProvider.SetEnqueueTime(OSConstants.BlockSyncAttachQueueName,
                    TimestampHelper.GetUtcNow());
                result = _blockSyncQueueService.ValidateQueueAvailability(OSConstants.BlockSyncAttachQueueName);
                result.ShouldBeTrue();

                _blockSyncStateProvider.SetEnqueueTime(OSConstants.BlockSyncAttachQueueName,
                    TimestampHelper.GetUtcNow()
                        .AddMilliseconds(-BlockSyncConstants.BlockSyncAttachBlockAgeLimit - 1000));
                result = _blockSyncQueueService.ValidateQueueAvailability(OSConstants.BlockSyncAttachQueueName);
                result.ShouldBeFalse();
            }

            {
                var result = _blockSyncQueueService.ValidateQueueAvailability(KernelConstants.UpdateChainQueueName);
                result.ShouldBeTrue();

                _blockSyncStateProvider.SetEnqueueTime(KernelConstants.UpdateChainQueueName,
                    TimestampHelper.GetUtcNow());
                result = _blockSyncQueueService.ValidateQueueAvailability(KernelConstants.UpdateChainQueueName);
                result.ShouldBeTrue();

                _blockSyncStateProvider.SetEnqueueTime(KernelConstants.UpdateChainQueueName,
                    TimestampHelper.GetUtcNow()
                        .AddMilliseconds(-BlockSyncConstants.BlockSyncAttachAndExecuteBlockAgeLimit - 1000));
                result = _blockSyncQueueService.ValidateQueueAvailability(KernelConstants.UpdateChainQueueName);
                result.ShouldBeFalse();
            }

            Assert.Throws<InvalidOperationException>(() =>
                _blockSyncQueueService.ValidateQueueAvailability("InvalidQueueName"));
        }

        [Fact]
        public void Enqueue_Test()
        {
            var result = false;

            _blockSyncQueueService.Enqueue(() =>
            {
                result = true;
                return Task.CompletedTask;
            }, OSConstants.BlockFetchQueueName);
            
            result.ShouldBeTrue();
            
            _blockSyncStateProvider.GetEnqueueTime(OSConstants.BlockFetchQueueName).ShouldBeNull();
        }
    }
}