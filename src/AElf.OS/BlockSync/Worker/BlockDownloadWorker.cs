using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.BlockSync.Types;
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
        private readonly IBlockDownloadJobStore _blockDownloadJobStore;

        public BlockDownloadWorker(AbpTimer timer,
            IBlockchainService blockchainService,
            IBlockDownloadService blockDownloadService,
            IBlockDownloadJobStore blockDownloadJobStore)
            : base(timer)
        {
            Timer.Period = 1000;

            _blockchainService = blockchainService;
            _blockDownloadService = blockDownloadService;
            _blockDownloadJobStore = blockDownloadJobStore;
        }

        protected override void DoWork()
        {
            AsyncHelper.RunSync(DownloadBlocksAsync);
        }

        private async Task DownloadBlocksAsync()
        {
            var blockDownloadJob = await _blockDownloadJobStore.GetFirstWaitingJobAsync();

            if (blockDownloadJob != null)
                return;

            var chain = await _blockchainService.GetChainAsync();

            if (chain.BestChainHeight >= blockDownloadJob.TargetBlockHeight)
                return;
            
            var syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(chain.LongestChainHash,
                chain.LongestChainHeight, blockDownloadJob.BatchRequestBlockCount, blockDownloadJob.SuggestedPeerPubkey);

            if (syncBlockCount == 0)
            {
                syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(chain.BestChainHash,
                    chain.BestChainHeight, blockDownloadJob.BatchRequestBlockCount, blockDownloadJob.SuggestedPeerPubkey);
            }

            if (syncBlockCount == 0)
            {
                Logger.LogDebug(
                    $"Resynchronize from lib, lib height: {chain.LastIrreversibleBlockHeight}.");
                await _blockDownloadService.DownloadBlocksAsync(
                    chain.LastIrreversibleBlockHash, chain.LastIrreversibleBlockHeight,
                    blockDownloadJob.BatchRequestBlockCount, blockDownloadJob.SuggestedPeerPubkey);
            }

        }

        private async Task<BlockDownloadJobInfo> GetFirstAvailableWaitingJobAsync(Chain chain)
        {
            var blockDownloadJob = await _blockDownloadJobStore.GetFirstWaitingJobAsync();

            if (blockDownloadJob == null)
                return null;

            if (blockDownloadJob.TargetBlockHeight > chain.BestChainHeight)
                return blockDownloadJob;

            return await GetFirstAvailableWaitingJobAsync(chain);
        }
    }
}