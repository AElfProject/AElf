using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.OS.BlockSync.Domain;
using AElf.OS.Network.Application;
using AElf.OS.Network.Extensions;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Worker
{
    public class BlockDownloadWorkerForkedTests : BlockSyncForkedTestBase
    {
        private readonly BlockDownloadWorker _blockDownloadWorker;
        private readonly IBlockDownloadJobManager _blockDownloadJobManager;
        private readonly IBlockchainService _blockchainService;
        private readonly INetworkService _networkService;

        public BlockDownloadWorkerForkedTests()
        {
            _blockDownloadWorker = GetRequiredService<BlockDownloadWorker>();
            _blockDownloadJobManager = GetRequiredService<IBlockDownloadJobManager>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _networkService = GetRequiredService<INetworkService>();
        }

        [Fact]
        public async Task ProcessDownloadJob_Success()
        {
            var chain = await _blockchainService.GetChainAsync();
            var originalBestChainHash = chain.BestChainHash;
            var originalBestChainHeight = chain.BestChainHeight;
            var response = await _networkService.GetBlocksAsync(chain.LastIrreversibleBlockHash, 30, null);
            var peerBlocks = response.Payload;
            
            var peerBlock = peerBlocks.Last();

            await _blockDownloadJobManager.EnqueueAsync(peerBlock.GetHash(), peerBlock.Height, 5, null);

            await _blockDownloadWorker.ProcessDownloadJobAsync();

            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(30);
            chain.BestChainHash.ShouldBe(peerBlock.GetHash());

            var block = await _blockchainService.GetBlockByHeightInBestChainBranchAsync(originalBestChainHeight);
            block.GetHash().ShouldNotBe(originalBestChainHash);
        }
        
        [Fact]
        public async Task ProcessDownloadJob_SyncFromLongestChain_Success()
        {
            var chain = await _blockchainService.GetChainAsync();
            var originalBestChainHash = chain.BestChainHash;
            var originalBestChainHeight = chain.BestChainHeight;
            var response = await _networkService.GetBlocksAsync(chain.LastIrreversibleBlockHash, 30, null);
            var peerBlocks = response.Payload;

            foreach (var peerBlockWithTransaction in peerBlocks)
            {
                chain = await _blockchainService.GetChainAsync();
                var peerBlock = peerBlockWithTransaction.ToBlock();
                await _blockchainService.AddBlockAsync(peerBlock);
                var result = await _blockchainService.AttachBlockToChainAsync(chain, peerBlock);
                if (result == BlockAttachOperationStatus.LongestChainFound)
                    break;
            }

            var lastPeerBlock = peerBlocks.Last();
            await _blockDownloadJobManager.EnqueueAsync(lastPeerBlock.GetHash(), lastPeerBlock.Height, 5, null);

            await _blockDownloadWorker.ProcessDownloadJobAsync();

            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(30);
            chain.BestChainHash.ShouldBe(lastPeerBlock.GetHash());

            var block = await _blockchainService.GetBlockByHeightInBestChainBranchAsync(originalBestChainHeight);
            block.GetHash().ShouldNotBe(originalBestChainHash);
        }
    }
}