using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using Moq;
using Xunit;

namespace AElf.OS.Handlers
{
    public class BlockMinedEventHandlerTests : NetworkBroadcastTestBase
    {
        private readonly BlockMinedEventHandler _blockMinedEventHandler;
        private readonly OSTestHelper _osTestHelper;
        private readonly IBlockchainService _blockchainService;
        private readonly NetworkServicePropagationTestContext _testContext;
        private readonly INodeSyncStateProvider _syncStateProvider;

        public BlockMinedEventHandlerTests()
        {
            _blockMinedEventHandler = GetRequiredService<BlockMinedEventHandler>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _testContext = GetRequiredService<NetworkServicePropagationTestContext>();
            _syncStateProvider = GetRequiredService<INodeSyncStateProvider>();
        }

        [Fact]
        public async Task HandleEvent_Test()
        {
            var blockHeader = _osTestHelper.GenerateBlock(HashHelper.ComputeFrom("BlockHash"), 100).Header;
            await HandleEventAsync(new BlockMinedEventData {BlockHeader = blockHeader});
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueBlock(It.Is<BlockWithTransactions>(b => b.GetHash() == blockHeader.GetHash()),
                    It.IsAny<Action<NetworkException>>()), Times.Never);

            _syncStateProvider.SetSyncTarget(-1);
            
            await HandleEventAsync(null);
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueBlock(It.Is<BlockWithTransactions>(b => b.GetHash() == blockHeader.GetHash()),
                    It.IsAny<Action<NetworkException>>()), Times.Never);
            
            await HandleEventAsync(new BlockMinedEventData());
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueBlock(It.Is<BlockWithTransactions>(b => b.GetHash() == blockHeader.GetHash()),
                    It.IsAny<Action<NetworkException>>()), Times.Never);
            
            await HandleEventAsync(new BlockMinedEventData {BlockHeader = blockHeader});
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueBlock(It.Is<BlockWithTransactions>(b => b.GetHash() == blockHeader.GetHash()),
                    It.IsAny<Action<NetworkException>>()), Times.Never);

            var bestChainHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();
            await HandleEventAsync(new BlockMinedEventData {BlockHeader = bestChainHeader});
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueBlock(It.Is<BlockWithTransactions>(b => b.GetHash() == bestChainHeader.GetHash()),
                    It.IsAny<Action<NetworkException>>()), Times.Once);
        }

        private async Task HandleEventAsync(BlockMinedEventData blockMinedEventData)
        {
            await _blockMinedEventHandler.HandleEventAsync(blockMinedEventData);
            await Task.Delay(500);
        }
    }
}