using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.BlockSync.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Infrastructure
{
    public class InMemoryBlockDownloadJobStoreTests : BlockSyncTestBase
    {
        private readonly IBlockDownloadJobStore _blockDownloadJobStore;

        public InMemoryBlockDownloadJobStoreTests()
        {
            _blockDownloadJobStore = GetRequiredService<IBlockDownloadJobStore>();
        }

        [Fact]
        public async Task Add_Test()
        {
            for (int i = 0; i < 100; i++)
            {
                var job = CreateBlockDownloadJob(i.ToString());

                var addResult = await _blockDownloadJobStore.AddAsync(job);
                addResult.ShouldBeTrue();
            }
            
            var moreThanLimitResult = await _blockDownloadJobStore.AddAsync(CreateBlockDownloadJob("10"));
            moreThanLimitResult.ShouldBeFalse();
        }

        [Fact]
        public async Task GetFirstWaitingJob_Test()
        {
            var job = await _blockDownloadJobStore.GetFirstWaitingJobAsync();
            job.ShouldBeNull();

            await _blockDownloadJobStore.AddAsync(CreateBlockDownloadJob("1"));
            await _blockDownloadJobStore.AddAsync(CreateBlockDownloadJob("2"));
            await _blockDownloadJobStore.AddAsync(CreateBlockDownloadJob("3"));
            
            job = await _blockDownloadJobStore.GetFirstWaitingJobAsync();
            job.JobId.ShouldBe("1");
        }

        [Fact]
        public async Task Update_Test()
        {
            var currentTargetBlockHash = HashHelper.ComputeFrom("CurrentTargetBlockHash");
            var currentTargetBlockHeight = 10;
            var deadline = TimestampHelper.GetUtcNow();
            
            await _blockDownloadJobStore.AddAsync(CreateBlockDownloadJob("1"));
            
            var job = await _blockDownloadJobStore.GetFirstWaitingJobAsync();
            job.CurrentTargetBlockHash = currentTargetBlockHash;
            job.CurrentTargetBlockHeight = currentTargetBlockHeight;
            job.IsFinished = true;
            job.Deadline = deadline;

            await _blockDownloadJobStore.UpdateAsync(job);
            
            job = await _blockDownloadJobStore.GetFirstWaitingJobAsync();
            job.CurrentTargetBlockHash.ShouldBe(currentTargetBlockHash);
            job.CurrentTargetBlockHeight.ShouldBe(currentTargetBlockHeight);
            job.IsFinished.ShouldBeTrue();
            job.Deadline.ShouldBe(deadline);
        }

        [Fact]
        public async Task Remove_Test()
        {
            await _blockDownloadJobStore.AddAsync(CreateBlockDownloadJob("1"));
            await _blockDownloadJobStore.RemoveAsync("1");
            var job = await _blockDownloadJobStore.GetFirstWaitingJobAsync();
            job.ShouldBeNull();
        }

        private BlockDownloadJobInfo CreateBlockDownloadJob(string jobId)
        {
            return new BlockDownloadJobInfo
            {
                JobId = jobId,
                BatchRequestBlockCount = 10,
                TargetBlockHash = HashHelper.ComputeFrom("TargetBlockHash"),
                TargetBlockHeight = 100,
                SuggestedPeerPubkey = "SuggestedPeerPubkey"
            };
        }
    }
}