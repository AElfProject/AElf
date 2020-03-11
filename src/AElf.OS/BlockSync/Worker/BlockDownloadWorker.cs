using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Application;
using AElf.OS.BlockSync.Dto;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.BlockSync.Types;
using AElf.Sdk.CSharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.OS.BlockSync.Worker
{
    public class BlockDownloadWorker : AsyncPeriodicBackgroundWorkerBase
    {
        private readonly BlockSyncOptions _blockSyncOptions;

        public BlockDownloadWorker(AbpTimer timer,
            IServiceScopeFactory serviceScopeFactory,
            IOptionsSnapshot<BlockSyncOptions> blockSyncOptions)
            : base(timer, serviceScopeFactory)
        {
            _blockSyncOptions = blockSyncOptions.Value;
            
            Timer.Period = _blockSyncOptions.BlockDownloadTimerPeriod;
        }

        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            await ProcessDownloadJobAsync(workerContext);
        }

        internal async Task ProcessDownloadJobAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            var blockchainService = workerContext.ServiceProvider.GetRequiredService<IBlockchainService>();
            var blockDownloadService = workerContext.ServiceProvider.GetRequiredService<IBlockDownloadService>();
            var blockDownloadJobStore = workerContext.ServiceProvider.GetRequiredService<IBlockDownloadJobStore>();
            
            while (true)
            {
                var chain = await blockchainService.GetChainAsync();
                var jobInfo = await GetFirstAvailableWaitingJobAsync(workerContext, chain);

                try
                {
                    if (!ValidateBeforeDownload(workerContext, jobInfo))
                    {
                        return;
                    }

                    if (jobInfo.IsFinished)
                    {
                        await RemoveJobAndTargetStateAsync(workerContext, jobInfo);
                        continue;
                    }

                    Logger.LogDebug(
                        $"Execute download job: CurrentTargetBlockHeight: {jobInfo.CurrentTargetBlockHeight}, TargetBlockHeight:{jobInfo.TargetBlockHeight}, SuggestedPeerPubkey:{jobInfo.SuggestedPeerPubkey}.");

                    var downloadResult = await DownloadBlocksAsync(workerContext, chain, jobInfo);

                    if (downloadResult.DownloadBlockCount == 0)
                    {
                        Logger.LogDebug(
                            $"Download block job finished: CurrentTargetBlockHeight: {jobInfo.CurrentTargetBlockHeight}, TargetBlockHeight:{jobInfo.TargetBlockHeight}.");
                        await RemoveJobAndTargetStateAsync(workerContext, jobInfo);
                        continue;
                    }
                
                    blockDownloadService.RemoveDownloadJobTargetState(jobInfo.CurrentTargetBlockHash);
                
                    jobInfo.Deadline = TimestampHelper.GetUtcNow().AddMilliseconds(downloadResult.DownloadBlockCount * 300);
                    jobInfo.CurrentTargetBlockHash = downloadResult.LastDownloadBlockHash;
                    jobInfo.CurrentTargetBlockHeight = downloadResult.LastDownloadBlockHeight;
                    jobInfo.IsFinished = downloadResult.DownloadBlockCount < _blockSyncOptions.MaxBlockDownloadCount;
                    await blockDownloadJobStore.UpdateAsync(jobInfo);

                    Logger.LogDebug(
                        $"Current download block job finished: CurrentTargetBlockHeight: {jobInfo.CurrentTargetBlockHeight}.");
                    return;
                }
                catch (Exception e)
                {
                    await RemoveJobAndTargetStateAsync(workerContext, jobInfo);
                    Logger.LogError(e,"Handle download job failed.");
                    throw;
                }
            }
        }
        
        private async Task<BlockDownloadJobInfo> GetFirstAvailableWaitingJobAsync(PeriodicBackgroundWorkerContext workerContext, Chain chain)
        {
            var blockDownloadJobStore = workerContext.ServiceProvider.GetRequiredService<IBlockDownloadJobStore>();
            while (true)
            {
                var blockDownloadJob = await blockDownloadJobStore.GetFirstWaitingJobAsync();

                if (blockDownloadJob == null)
                    return null;

                if (blockDownloadJob.CurrentTargetBlockHeight == 0 &&
                    blockDownloadJob.TargetBlockHeight <= chain.BestChainHeight)
                {
                    await blockDownloadJobStore.RemoveAsync(blockDownloadJob.JobId);
                    continue;
                }

                return blockDownloadJob;
            }
        }

        private bool ValidateBeforeDownload(PeriodicBackgroundWorkerContext workerContext, BlockDownloadJobInfo blockDownloadJobInfo)
        {
            var blockDownloadService = workerContext.ServiceProvider.GetRequiredService<IBlockDownloadService>();
            
            if (blockDownloadJobInfo == null)
                return false;

            if (!blockDownloadService.ValidateQueueAvailabilityBeforeDownload())
                return false;

            if (blockDownloadJobInfo.CurrentTargetBlockHash != null
                && blockDownloadService.IsNotReachedDownloadTarget(blockDownloadJobInfo.CurrentTargetBlockHash)
                && TimestampHelper.GetUtcNow() < blockDownloadJobInfo.Deadline)
                return false;

            return true;
        }

        private async Task<DownloadBlocksResult> DownloadBlocksAsync(PeriodicBackgroundWorkerContext workerContext, Chain chain, BlockDownloadJobInfo jobInfo)
        {
            var blockDownloadService = workerContext.ServiceProvider.GetRequiredService<IBlockDownloadService>();

            var downloadResult = new DownloadBlocksResult();
            if (jobInfo.CurrentTargetBlockHeight == 0)
            {
                // Download blocks from longest chain
                downloadResult = await blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
                {
                    PreviousBlockHash = chain.LongestChainHash,
                    PreviousBlockHeight = chain.LongestChainHeight,
                    BatchRequestBlockCount = jobInfo.BatchRequestBlockCount,
                    SuggestedPeerPubkey = jobInfo.SuggestedPeerPubkey,
                    MaxBlockDownloadCount = _blockSyncOptions.MaxBlockDownloadCount,
                    UseSuggestedPeer = true
                });
                // Download blocks from best chain
                if (downloadResult.Success && downloadResult.DownloadBlockCount == 0)
                {
                    downloadResult = await blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
                    {
                        PreviousBlockHash = chain.BestChainHash,
                        PreviousBlockHeight = chain.BestChainHeight,
                        BatchRequestBlockCount = jobInfo.BatchRequestBlockCount,
                        SuggestedPeerPubkey = jobInfo.SuggestedPeerPubkey,
                        MaxBlockDownloadCount = _blockSyncOptions.MaxBlockDownloadCount,
                        UseSuggestedPeer = true
                    });
                }

                // Download blocks from LIB
                if (downloadResult.Success && downloadResult.DownloadBlockCount == 0)
                {
                    Logger.LogDebug($"Resynchronize from lib, lib height: {chain.LastIrreversibleBlockHeight}.");
                    downloadResult = await blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
                    {
                        PreviousBlockHash = chain.LastIrreversibleBlockHash,
                        PreviousBlockHeight = chain.LastIrreversibleBlockHeight,
                        BatchRequestBlockCount = jobInfo.BatchRequestBlockCount,
                        SuggestedPeerPubkey = jobInfo.SuggestedPeerPubkey,
                        MaxBlockDownloadCount = _blockSyncOptions.MaxBlockDownloadCount,
                        UseSuggestedPeer = true
                    });
                }

            }
            // If last target block didn't become the longest chain, stop this job.
            else if (jobInfo.CurrentTargetBlockHeight <= chain.LongestChainHeight + 8)
            {
                downloadResult = await blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
                {
                    PreviousBlockHash = jobInfo.CurrentTargetBlockHash,
                    PreviousBlockHeight = jobInfo.CurrentTargetBlockHeight,
                    BatchRequestBlockCount = jobInfo.BatchRequestBlockCount,
                    SuggestedPeerPubkey = jobInfo.SuggestedPeerPubkey,
                    MaxBlockDownloadCount = _blockSyncOptions.MaxBlockDownloadCount,
                    UseSuggestedPeer = false
                });
            }

            return downloadResult;
        }

        private async Task RemoveJobAndTargetStateAsync(PeriodicBackgroundWorkerContext workerContext, BlockDownloadJobInfo blockDownloadJobInfo)
        {
            var blockDownloadService = workerContext.ServiceProvider.GetRequiredService<IBlockDownloadService>();
            var blockDownloadJobStore = workerContext.ServiceProvider.GetRequiredService<IBlockDownloadJobStore>();
            
            await blockDownloadJobStore.RemoveAsync(blockDownloadJobInfo.JobId);
            blockDownloadService.RemoveDownloadJobTargetState(blockDownloadJobInfo.CurrentTargetBlockHash);
        }
    }
}