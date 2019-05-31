using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public class BlockFetchServiceTests : BlockSyncTestBase
    {
        private readonly IBlockFetchService _blockFetchService;
        private readonly INetworkService _networkService;
        private readonly ITaskQueueManager _taskQueueManager;
        private readonly BlockSyncTestHelper _blockSyncTestHelper;
        private readonly IBlockchainService _blockchainService;

        public BlockFetchServiceTests()
        {
            _blockFetchService = GetRequiredService<IBlockFetchService>();
            _networkService = GetRequiredService<INetworkService>();
            _blockSyncTestHelper = GetRequiredService<BlockSyncTestHelper>();
            _blockchainService = GetRequiredService<IBlockchainService>();
        }

        [Fact]
        public async Task FetchBlock_Success()
        {
            var peerBlock = await _networkService.GetBlockByHashAsync(Hash.FromString("PeerBlock"));

            var block = await _blockchainService.GetBlockByHashAsync(peerBlock.GetHash());
            block.ShouldBeNull();

            var fetchResult = await _blockFetchService.FetchBlockAsync(peerBlock.GetHash(), peerBlock.Height, null);
            _blockSyncTestHelper.DisposeQueue();
            
            fetchResult.ShouldBeTrue();
            
            block = await _blockchainService.GetBlockByHashAsync(peerBlock.GetHash());
            block.GetHash().ShouldBe(peerBlock.GetHash());
        }

        [Fact]
        public async Task FetchBlock_AlreadyExist_Success()
        {
            var chain = await _blockchainService.GetChainAsync();
            var fetchResult = await _blockFetchService.FetchBlockAsync(chain.BestChainHash, chain.BestChainHeight, null);
            _blockSyncTestHelper.DisposeQueue();
            
            fetchResult.ShouldBeTrue();
        }

        [Fact]
        public async Task FetchBlock_ReturnNull_Failure()
        {
            var fetchResult = await _blockFetchService.FetchBlockAsync(Hash.Empty, 0, null);
            _blockSyncTestHelper.DisposeQueue();
            
            fetchResult.ShouldBeFalse();
        }
    }
}