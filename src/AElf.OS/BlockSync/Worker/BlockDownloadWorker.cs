using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.BlockSync.Types;
using AElf.Sdk.CSharp;
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
            Timer.Period = 1000;

            _blockchainService = blockchainService;
            _blockDownloadService = blockDownloadService;
            _blockSyncStateProvider = blockSyncStateProvider;
            _blockDownloadJobStore = blockDownloadJobStore;
        }

        protected override void DoWork()
        {
            AsyncHelper.RunSync(DownloadBlocksAsync);
        }

        private async Task DownloadBlocksAsync()
        {
            var chain = await _blockchainService.GetChainAsync();
            var jobInfo = await GetFirstAvailableWaitingJobAsync(chain);

            if (!ValidateBeforeDownload(jobInfo))
                return;

            DownloadBlocksResult downloadBlocksResult;
            if (await _blockchainService.GetBlockHashByHeightAsync(chain, jobInfo.CurrentTargetBlockHeight,
                    chain.BestChainHash) == jobInfo.CurrentTargetBlockHash)
            {
                downloadBlocksResult = await _blockDownloadService.DownloadBlocksAsync(jobInfo.CurrentTargetBlockHash,
                    jobInfo.CurrentTargetBlockHeight, jobInfo.BatchRequestBlockCount, jobInfo.SuggestedPeerPubkey);
            }
            else
            {
                downloadBlocksResult = await _blockDownloadService.DownloadBlocksAsync(chain.LongestChainHash,
                    chain.LongestChainHeight, jobInfo.BatchRequestBlockCount, jobInfo.SuggestedPeerPubkey);

                if (downloadBlocksResult.DownloadBlockCount == 0)
                {
                    downloadBlocksResult = await _blockDownloadService.DownloadBlocksAsync(chain.BestChainHash,
                        chain.BestChainHeight, jobInfo.BatchRequestBlockCount, jobInfo.SuggestedPeerPubkey);
                }

                if (downloadBlocksResult.DownloadBlockCount == 0)
                {
                    Logger.LogDebug(
                        $"Resynchronize from lib, lib height: {chain.LastIrreversibleBlockHeight}.");
                    downloadBlocksResult = await _blockDownloadService.DownloadBlocksAsync(
                        chain.LastIrreversibleBlockHash, chain.LastIrreversibleBlockHeight,
                        jobInfo.BatchRequestBlockCount, jobInfo.SuggestedPeerPubkey);
                }
            }

            if (downloadBlocksResult.DownloadBlockCount == 0 ||
                downloadBlocksResult.LastDownloadBlockHeight >= jobInfo.TargetBlockHeight)
            {
                await _blockDownloadJobStore.RemoveAsync(jobInfo.JobId);
                _blockSyncStateProvider.DownloadJobTargetState.TryRemove(jobInfo.TargetBlockHash, out _);
                return;
            }

            _blockSyncStateProvider.DownloadJobTargetState[jobInfo.TargetBlockHash] = false;

            jobInfo.Deadline = TimestampHelper.GetUtcNow().AddMilliseconds(
                (downloadBlocksResult.DownloadBlockCount > 600 ? 600 : downloadBlocksResult.DownloadBlockCount) * 300);
            jobInfo.CurrentTargetBlockHash = downloadBlocksResult.LastDownloadBlockHash;
            jobInfo.CurrentTargetBlockHeight = downloadBlocksResult.LastDownloadBlockHeight;

            await _blockDownloadJobStore.UpdateAsync(jobInfo);
        }

        private bool ValidateBeforeDownload(BlockDownloadJobInfo blockDownloadJobInfo)
        {
            if (blockDownloadJobInfo == null)
                return false;

            if (!_blockDownloadService.ValidateQueueAvailability())
                return false;

            if (_blockSyncStateProvider.DownloadJobTargetState.TryGetValue(blockDownloadJobInfo.TargetBlockHash,
                    out var state) && state == false && TimestampHelper.GetUtcNow() < blockDownloadJobInfo.Deadline)
                return false;

            return true;
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
                    await _blockDownloadJobStore.RemoveAsync(blockDownloadJob.JobId);
                    continue;
                }

                return blockDownloadJob;
            }
        }
    }
}