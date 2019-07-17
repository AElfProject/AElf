using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.BlockSync.Types;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.OS.BlockSync.Worker
{
    public class BlockDownloadWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockDownloadService _blockDownloadService;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;
        private readonly IBlockDownloadJobStore _blockDownloadJobStore;

        public BlockDownloadWorker(AbpTimer timer,
            IBlockchainService blockchainService,
            IBlockDownloadService blockDownloadService,
            IBlockSyncStateProvider blockSyncStateProvider,
            IBlockDownloadJobStore blockDownloadJobStore)
            : base(timer)
        {
            Timer.Period = BlockSyncConstants.BlockDownloadTimerPeriod;

            _blockchainService = blockchainService;
            _blockDownloadService = blockDownloadService;
            _blockSyncStateProvider = blockSyncStateProvider;
            _blockDownloadJobStore = blockDownloadJobStore;
        }

        protected override void DoWork()
        {
            AsyncHelper.RunSync(ProcessDownloadJobAsync);
        }

        private async Task ProcessDownloadJobAsync()
        {
            while (true)
            {
                var chain = await _blockchainService.GetChainAsync();
                var jobInfo = await GetFirstAvailableWaitingJobAsync(chain);

                if (!ValidateBeforeDownload(jobInfo))
                {
                    return;
                }

                var downloadResult = await DownloadBlocksAsync(chain, jobInfo);

                if (downloadResult.DownloadBlockCount == 0)
                {
                    Logger.LogDebug(
                        $"Download block job finished: LastDownloadBlockHeight: {downloadResult.LastDownloadBlockHeight}, TargetBlockHeight:{jobInfo.TargetBlockHeight}.");
                    await RemoveJobAndTargetStateAsync(jobInfo);
                    continue;
                }

                _blockSyncStateProvider.DownloadJobTargetState.TryRemove(jobInfo.CurrentTargetBlockHash, out _);
                
                jobInfo.Deadline = TimestampHelper.GetUtcNow().AddMilliseconds(downloadResult.DownloadBlockCount * 300);
                jobInfo.CurrentTargetBlockHash = downloadResult.LastDownloadBlockHash;
                jobInfo.CurrentTargetBlockHeight = downloadResult.LastDownloadBlockHeight;
                await _blockDownloadJobStore.UpdateAsync(jobInfo);
                
                _blockSyncStateProvider.DownloadJobTargetState[jobInfo.CurrentTargetBlockHash] = false;

                Logger.LogDebug(
                    $"Current download block job finished: CurrentTargetBlockHeight: {jobInfo.CurrentTargetBlockHeight}.");
                return;
            }
        }
        
        private async Task<BlockDownloadJobInfo> GetFirstAvailableWaitingJobAsync(Chain chain)
        {
            while (true)
            {
                var blockDownloadJob = await _blockDownloadJobStore.GetFirstWaitingJobAsync();

                if (blockDownloadJob == null)
                    return null;

                if (blockDownloadJob.TargetBlockHeight <= chain.BestChainHeight)
                {
                    await RemoveJobAndTargetStateAsync(blockDownloadJob);
                    continue;
                }

                return blockDownloadJob;
            }
        }

        private bool ValidateBeforeDownload(BlockDownloadJobInfo blockDownloadJobInfo)
        {
            if (blockDownloadJobInfo == null)
                return false;

            if (!_blockDownloadService.ValidateQueueAvailabilityBeforeDownload())
                return false;

            if (blockDownloadJobInfo.CurrentTargetBlockHash != null
                && _blockSyncStateProvider.DownloadJobTargetState.TryGetValue(
                    blockDownloadJobInfo.CurrentTargetBlockHash, out var state)
                && state == false && TimestampHelper.GetUtcNow() < blockDownloadJobInfo.Deadline)
                return false;

            return true;
        }

        private async Task<DownloadBlocksResult> DownloadBlocksAsync(Chain chain, BlockDownloadJobInfo jobInfo)
        {
            var downloadResult = new DownloadBlocksResult();
            if (jobInfo.CurrentTargetBlockHeight > 0)
            {
                if (jobInfo.CurrentTargetBlockHeight <= chain.BestChainHeight || await BlockIsInBestChain(chain,
                        jobInfo.CurrentTargetBlockHash, jobInfo.CurrentTargetBlockHeight))
                {
                    downloadResult = await _blockDownloadService.DownloadBlocksAsync(jobInfo.CurrentTargetBlockHash,
                        jobInfo.CurrentTargetBlockHeight, jobInfo.BatchRequestBlockCount,
                        jobInfo.SuggestedPeerPubkey);
                }
            }
            else
            {
                downloadResult = await _blockDownloadService.DownloadBlocksAsync(chain.LongestChainHash,
                    chain.LongestChainHeight, jobInfo.BatchRequestBlockCount, jobInfo.SuggestedPeerPubkey);

                if (downloadResult.DownloadBlockCount == 0)
                {
                    downloadResult = await _blockDownloadService.DownloadBlocksAsync(chain.BestChainHash,
                        chain.BestChainHeight, jobInfo.BatchRequestBlockCount, jobInfo.SuggestedPeerPubkey);
                }

                if (downloadResult.DownloadBlockCount == 0)
                {
                    Logger.LogDebug($"Resynchronize from lib, lib height: {chain.LastIrreversibleBlockHeight}.");
                    downloadResult = await _blockDownloadService.DownloadBlocksAsync(
                        chain.LastIrreversibleBlockHash, chain.LastIrreversibleBlockHeight,
                        jobInfo.BatchRequestBlockCount, jobInfo.SuggestedPeerPubkey);
                }
            }

            return downloadResult;
        }

        private async Task<bool> BlockIsInBestChain(Chain chain, Hash blockHash, long blockHeight)
        {
            return await _blockchainService.GetBlockHashByHeightAsync(chain, blockHeight, chain.BestChainHash) ==
                   blockHash;
        }

        private async Task RemoveJobAndTargetStateAsync(BlockDownloadJobInfo blockDownloadJobInfo)
        {
            await _blockDownloadJobStore.RemoveAsync(blockDownloadJobInfo.JobId);
            _blockSyncStateProvider.DownloadJobTargetState.TryRemove(blockDownloadJobInfo.CurrentTargetBlockHash, out _);
        }
    }
}