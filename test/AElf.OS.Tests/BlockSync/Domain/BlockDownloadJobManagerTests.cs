using System.Threading.Tasks;
using AElf.OS.BlockSync.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Domain
{
    public class BlockDownloadJobManagerTests : BlockSyncTestBase
    {
        private readonly IBlockDownloadJobManager _blockDownloadJobManager;
        private readonly IBlockDownloadJobStore _blockDownloadJobStore;

        public BlockDownloadJobManagerTests()
        {
            _blockDownloadJobManager = GetRequiredService<IBlockDownloadJobManager>();
            _blockDownloadJobStore = GetRequiredService<IBlockDownloadJobStore>();
        }

        [Fact]
        public async Task Enqueue_Test()
        {
            var syncBlockHash = HashHelper.ComputeFrom("SyncBlockHash");
            var syncBlockHeight = 100;
            var batchRequestBlockCount = 10;
            var suggestedPeerPubkey = "SuggestedPeerPubkey";

            var jobId = await _blockDownloadJobManager.EnqueueAsync(syncBlockHash, syncBlockHeight,
                batchRequestBlockCount, suggestedPeerPubkey);
            jobId.ShouldNotBeNull();

            var jobInfo = await _blockDownloadJobStore.GetFirstWaitingJobAsync();
            jobInfo.TargetBlockHash.ShouldBe(syncBlockHash);
            jobInfo.TargetBlockHeight.ShouldBe(syncBlockHeight);
            jobInfo.BatchRequestBlockCount.ShouldBe(batchRequestBlockCount);
            jobInfo.SuggestedPeerPubkey.ShouldBe(suggestedPeerPubkey);

            for (int i = 0; i < 99; i++)
            {
                syncBlockHash = HashHelper.ComputeFrom("SyncBlockHash" + i);
                jobId = await _blockDownloadJobManager.EnqueueAsync(syncBlockHash, syncBlockHeight,
                    batchRequestBlockCount, suggestedPeerPubkey);
                jobId.ShouldNotBeNull();
            }
            
            syncBlockHash = HashHelper.ComputeFrom("SyncBlockHash100" );
            jobId = await _blockDownloadJobManager.EnqueueAsync(syncBlockHash, syncBlockHeight,
                batchRequestBlockCount, suggestedPeerPubkey);
            jobId.ShouldBeNull();
        }
    }
}