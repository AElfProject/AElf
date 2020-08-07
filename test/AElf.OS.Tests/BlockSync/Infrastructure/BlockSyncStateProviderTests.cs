using AElf.Kernel;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Infrastructure
{
    public class BlockSyncStateProviderTests : BlockSyncTestBase
    {
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;
        
        public BlockSyncStateProviderTests()
        {
            _blockSyncStateProvider = GetRequiredService<IBlockSyncStateProvider>();
        }

        [Fact]
        public void EnqueueTime_Test()
        {
            var testQueueName = "TestQueue";
            var testEnqueueTime = TimestampHelper.GetUtcNow();
            
            var enqueueTime = _blockSyncStateProvider.GetEnqueueTime(testQueueName);
            enqueueTime.ShouldBeNull();
            
            _blockSyncStateProvider.SetEnqueueTime(testQueueName, testEnqueueTime);
            
            enqueueTime = _blockSyncStateProvider.GetEnqueueTime(testQueueName);
            enqueueTime.ShouldBe(testEnqueueTime);
        }

        [Fact]
        public void DownloadJobTargetState_Test()
        {
            var targetHash = HashHelper.ComputeFrom("TargetHash");
            var result = _blockSyncStateProvider.TryUpdateDownloadJobTargetState(targetHash, false);
            result.ShouldBeFalse();

            _blockSyncStateProvider.SetDownloadJobTargetState(targetHash, false);
            result = _blockSyncStateProvider.TryGetDownloadJobTargetState(targetHash, out var value);
            result.ShouldBeTrue();
            value.ShouldBeFalse();
            
            result = _blockSyncStateProvider.TryUpdateDownloadJobTargetState(targetHash, false);
            result.ShouldBeFalse();
            
            result = _blockSyncStateProvider.TryUpdateDownloadJobTargetState(targetHash, true);
            result.ShouldBeTrue();

            _blockSyncStateProvider.TryRemoveDownloadJobTargetState(targetHash);
            result = _blockSyncStateProvider.TryGetDownloadJobTargetState(targetHash, out value);
            result.ShouldBeFalse();
        }
    }
}