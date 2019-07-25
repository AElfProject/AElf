using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Domain;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Worker
{
    public class BlockDownloadWorkerManyJobTests : BlockSyncManyJobsTestBase
    {
        private readonly BlockDownloadWorker _blockDownloadWorker;
        private readonly IBlockDownloadJobStore _blockDownloadJobStore;
        private readonly IBlockDownloadJobManager _blockDownloadJobManager;
        private readonly IBlockchainService _blockchainService;
        private readonly INetworkService _networkService;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;
        private readonly BlockSyncOptions _blockSyncOptions;

        public BlockDownloadWorkerManyJobTests()
        {
            _blockDownloadWorker = GetRequiredService<BlockDownloadWorker>();
            _blockDownloadJobStore = GetRequiredService<IBlockDownloadJobStore>();
            _blockDownloadJobManager = GetRequiredService<IBlockDownloadJobManager>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _networkService = GetRequiredService<INetworkService>();
            _blockSyncStateProvider = GetRequiredService<IBlockSyncStateProvider>();
            _blockSyncOptions = GetRequiredService<IOptionsSnapshot<BlockSyncOptions>>().Value;
        }

        [Fact]
        public async Task ProcessDownloadJob_ManyJob()
        {
            var chain = await _blockchainService.GetChainAsync();
            var peerBlocks = await _networkService.GetBlocksAsync(chain.BestChainHash, 30);

            // Enqueue download job, from height 25 to 31
            for (int i = 13; i < 19; i++)
            {
                await _blockDownloadJobManager.EnqueueAsync(peerBlocks[i].GetHash(), peerBlocks[i].Height,
                    _blockSyncOptions.MaxBatchRequestBlockCount, null);
            }

            {
                // Worker run once
                // Execute job(TargetBlockHeight: 25)
                // BestChainHeight should be 14
                await RunWorkerAsync(1, peerBlocks);
                chain = await _blockchainService.GetChainAsync();
                chain.BestChainHeight.ShouldBe(peerBlocks[2].Height);
                chain.BestChainHash.ShouldBe(peerBlocks[2].GetHash());

                var jobInfo = await _blockDownloadJobStore.GetFirstWaitingJobAsync();
                jobInfo.TargetBlockHeight.ShouldBe(peerBlocks[13].Height);
                jobInfo.TargetBlockHash.ShouldBe(peerBlocks[13].GetHash());
            }

            {
                // Worker run 4 times
                // Execute job(TargetBlockHeight: 25)
                // BestChainHeight should be 26
                await RunWorkerAsync(4, peerBlocks);
                chain = await _blockchainService.GetChainAsync();
                chain.BestChainHeight.ShouldBe(peerBlocks[14].Height);
                chain.BestChainHash.ShouldBe(peerBlocks[14].GetHash());

                var jobInfo = await _blockDownloadJobStore.GetFirstWaitingJobAsync();
                jobInfo.TargetBlockHeight.ShouldBe(peerBlocks[13].Height);
                jobInfo.TargetBlockHash.ShouldBe(peerBlocks[13].GetHash());
            }

            {
                // Worker run once
                // Execute job(TargetBlockHeight: 25): Just drop the job
                // Execute job(TargetBlockHeight: 26): Just drop the job
                // Execute job(TargetBlockHeight: 27)
                // BestChainHeight should be 29
                await RunWorkerAsync(1, peerBlocks);
                chain = await _blockchainService.GetChainAsync();
                chain.BestChainHeight.ShouldBe(peerBlocks[17].Height);
                chain.BestChainHash.ShouldBe(peerBlocks[17].GetHash());

                var jobInfo = await _blockDownloadJobStore.GetFirstWaitingJobAsync();
                jobInfo.TargetBlockHeight.ShouldBe(peerBlocks[15].Height);
                jobInfo.TargetBlockHash.ShouldBe(peerBlocks[15].GetHash());
            }

            {
                // Worker run once
                // Execute job(TargetBlockHeight: 27): Just drop the job
                // Execute job(TargetBlockHeight: 28): Just drop the job
                // Execute job(TargetBlockHeight: 29): Just drop the job
                // Execute job(TargetBlockHeight: 30)
                // BestChainHeight should be 31
                await RunWorkerAsync(1, peerBlocks);
                chain = await _blockchainService.GetChainAsync();
                chain.BestChainHeight.ShouldBe(peerBlocks[19].Height);
                chain.BestChainHash.ShouldBe(peerBlocks[19].GetHash());

                var jobInfo = await _blockDownloadJobStore.GetFirstWaitingJobAsync();
                jobInfo.TargetBlockHeight.ShouldBe(peerBlocks[18].Height);
                jobInfo.TargetBlockHash.ShouldBe(peerBlocks[18].GetHash());
            }

            {
                // Worker run once
                // Execute job(TargetBlockHeight: 30): Just drop the job
                // Execute job(TargetBlockHeight: 31): Just drop the job
                // BestChainHeight should be 31
                await RunWorkerAsync(1, peerBlocks);
                chain = await _blockchainService.GetChainAsync();
                chain.BestChainHeight.ShouldBe(peerBlocks[19].Height);
                chain.BestChainHash.ShouldBe(peerBlocks[19].GetHash());

                var jobInfo = await _blockDownloadJobStore.GetFirstWaitingJobAsync();
                jobInfo.ShouldBeNull();
            }
        }

        private async Task RunWorkerAsync(int times, List<BlockWithTransactions> peerBlocks)
        {
            for (int i = 0; i < times; i++)
            {
                foreach (var block in peerBlocks)
                {
                    _blockSyncStateProvider.TryRemoveDownloadJobTargetState(block.GetHash());
                }

                await _blockDownloadWorker.ProcessDownloadJobAsync();
            }
        }
    }
}