using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Application;
using AElf.OS.BlockSync.Dto;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.BlockSync.Types;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

[assembly: InternalsVisibleTo("AElf.OS.Tests")]

namespace AElf.OS.BlockSync.Worker
{
    public class BlockDownloadWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockDownloadService _blockDownloadService;
        private readonly IBlockDownloadJobStore _blockDownloadJobStore;
        private readonly BlockSyncOptions _blockSyncOptions;

        public BlockDownloadWorker(AbpTimer timer,
            IBlockchainService blockchainService,
            IBlockDownloadService blockDownloadService,
            IBlockDownloadJobStore blockDownloadJobStore,
            IOptionsSnapshot<BlockSyncOptions> blockSyncOptions)
            : base(timer)
        {
            _blockchainService = blockchainService;
            _blockDownloadService = blockDownloadService;
            _blockDownloadJobStore = blockDownloadJobStore;
            _blockSyncOptions = blockSyncOptions.Value;
            
            Timer.Period = _blockSyncOptions.BlockDownloadTimerPeriod;
        }

        protected override void DoWork()
        {
            AsyncHelper.RunSync(ProcessDownloadJobAsync);
        }

        internal async Task ProcessDownloadJobAsync()
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
                        $"Download block job finished: CurrentTargetBlockHeight: {jobInfo.CurrentTargetBlockHeight}, TargetBlockHeight:{jobInfo.TargetBlockHeight}.");
                    await RemoveJobAndTargetStateAsync(jobInfo);
                    continue;
                }
                
                _blockDownloadService.RemoveDownloadJobTargetState(jobInfo.CurrentTargetBlockHash);
                
                jobInfo.Deadline = TimestampHelper.GetUtcNow().AddMilliseconds(downloadResult.DownloadBlockCount * 300);
                jobInfo.CurrentTargetBlockHash = downloadResult.LastDownloadBlockHash;
                jobInfo.CurrentTargetBlockHeight = downloadResult.LastDownloadBlockHeight;
                await _blockDownloadJobStore.UpdateAsync(jobInfo);

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
                && _blockDownloadService.IsNotReachedDownloadTarget(blockDownloadJobInfo.CurrentTargetBlockHash)
                && TimestampHelper.GetUtcNow() < blockDownloadJobInfo.Deadline)
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
                    downloadResult = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
                    {
                        PreviousBlockHash = jobInfo.CurrentTargetBlockHash,
                        PreviousBlockHeight = jobInfo.CurrentTargetBlockHeight,
                        BatchRequestBlockCount = jobInfo.BatchRequestBlockCount,
                        SuggestedPeerPubkey = jobInfo.SuggestedPeerPubkey,
                        MaxBlockDownloadCount = _blockSyncOptions.MaxBlockDownloadCount
                    });
                }
            }
            else
            {
                // Download blocks from longest chain
                downloadResult = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
                {
                    PreviousBlockHash = chain.LongestChainHash,
                    PreviousBlockHeight = chain.LongestChainHeight,
                    BatchRequestBlockCount = jobInfo.BatchRequestBlockCount,
                    SuggestedPeerPubkey = jobInfo.SuggestedPeerPubkey,
                    MaxBlockDownloadCount = _blockSyncOptions.MaxBlockDownloadCount
                });
                // Download blocks from best chain
                if (downloadResult.DownloadBlockCount == 0)
                {
                    downloadResult = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
                    {
                        PreviousBlockHash = chain.BestChainHash,
                        PreviousBlockHeight = chain.BestChainHeight,
                        BatchRequestBlockCount = jobInfo.BatchRequestBlockCount,
                        SuggestedPeerPubkey = jobInfo.SuggestedPeerPubkey,
                        MaxBlockDownloadCount = _blockSyncOptions.MaxBlockDownloadCount
                    });
                }
                // Download blocks from LIB
                if (downloadResult.DownloadBlockCount == 0)
                {
                    Logger.LogDebug($"Resynchronize from lib, lib height: {chain.LastIrreversibleBlockHeight}.");
                    downloadResult = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
                    {
                        PreviousBlockHash = chain.LastIrreversibleBlockHash,
                        PreviousBlockHeight = chain.LastIrreversibleBlockHeight,
                        BatchRequestBlockCount = jobInfo.BatchRequestBlockCount,
                        SuggestedPeerPubkey = jobInfo.SuggestedPeerPubkey,
                        MaxBlockDownloadCount = _blockSyncOptions.MaxBlockDownloadCount
                    });
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
            _blockDownloadService.RemoveDownloadJobTargetState(blockDownloadJobInfo.CurrentTargetBlockHash);
        }
    }
}