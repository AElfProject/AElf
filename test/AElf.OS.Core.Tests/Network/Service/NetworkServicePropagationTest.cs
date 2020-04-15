using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.OS.Helpers;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Moq;
using Xunit;

namespace AElf.OS.Network.Service
{
    public class NetworkServicePropagationTest : NetworkServicePropagationTestBase
    {
        private readonly NetworkServicePropagationTestContext _testContext;
        private readonly INetworkService _networkService;
        private readonly IPeerPool _peerPool;

        public NetworkServicePropagationTest()
        {
            _testContext = GetRequiredService<NetworkServicePropagationTestContext>();
            _networkService = GetRequiredService<INetworkService>();
            _peerPool = GetRequiredService<IPeerPool>();
        }

        [Fact]
        public async Task BroadcastBlockTest()
        {
            var blockHeader = OsCoreTestHelper.CreateFakeBlockHeader(chainId: 1, height: 2);
            var blockWithTx = new BlockWithTransactions { Header = blockHeader };

            await _networkService.BroadcastBlockWithTransactionsAsync(blockWithTx);
            
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.TryAddKnownBlock(blockHeader.GetHash()), Times.Once());
            
            await _networkService.BroadcastBlockWithTransactionsAsync(blockWithTx);

            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueBlock(blockWithTx, It.IsAny<Action<NetworkException>>()), Times.Once());
        }
        
        [Fact]
        public async Task BroadcastAnnouncementTest()
        {
            var blockHeader = OsCoreTestHelper.CreateFakeBlockHeader(chainId: 1, height: 2);

            await _networkService.BroadcastAnnounceAsync(blockHeader);
            
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.TryAddKnownBlock(blockHeader.GetHash()), Times.Once());
            
            await _networkService.BroadcastAnnounceAsync(blockHeader);
            
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueAnnouncement(It.Is<BlockAnnouncement>(ba => ba.BlockHash == blockHeader.GetHash()), 
                    It.IsAny<Action<NetworkException>>()), Times.Once());
        }

        [Fact]
        public async Task BroadcastTransactionTest()
        {
            var transaction = OsCoreTestHelper.CreateFakeTransaction();

            await _networkService.BroadcastTransactionAsync(transaction);

            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.TryAddKnownTransaction(transaction.GetHash()), Times.Once());
            
            await _networkService.BroadcastTransactionAsync(transaction);

            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueTransaction(It.Is<Transaction>(tx => tx.GetHash() == transaction.GetHash()),
                    It.IsAny<Action<NetworkException>>()), Times.Once());
        }

        [Fact]
        public async Task BroadcastBlock_OnePeerKnowsBlock_Test()
        {
            var blockHeader = OsCoreTestHelper.CreateFakeBlockHeader(1, 2);
            var blockWithTransactions = new BlockWithTransactions {Header = blockHeader};

            _peerPool.GetPeers().First().TryAddKnownBlock(blockWithTransactions.GetHash());

            await _networkService.BroadcastBlockWithTransactionsAsync(blockWithTransactions);

            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.TryAddKnownBlock(blockHeader.GetHash()), Times.Once());

            _testContext.MockedPeers[0].Verify(
                p => p.EnqueueBlock(blockWithTransactions, It.IsAny<Action<NetworkException>>()),
                Times.Never);
            _testContext.MockedPeers[1].Verify(
                p => p.EnqueueBlock(blockWithTransactions, It.IsAny<Action<NetworkException>>()),
                Times.Once());
            _testContext.MockedPeers[2].Verify(
                p => p.EnqueueBlock(blockWithTransactions, It.IsAny<Action<NetworkException>>()),
                Times.Once());
        }

        [Fact]
        public async Task BroadcastAnnouncement_OnePeerKnowsBlock_Test()
        {
            var blockHeader = OsCoreTestHelper.CreateFakeBlockHeader(1, 2);

            _peerPool.GetPeers().First().TryAddKnownBlock(blockHeader.GetHash());

            await _networkService.BroadcastAnnounceAsync(blockHeader);

            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.TryAddKnownBlock(blockHeader.GetHash()), Times.Once());

            _testContext.MockedPeers[0].Verify(p => p.EnqueueAnnouncement(
                It.Is<BlockAnnouncement>(ba => ba.BlockHash == blockHeader.GetHash()),
                It.IsAny<Action<NetworkException>>()), Times.Never);
            _testContext.MockedPeers[1].Verify(p => p.EnqueueAnnouncement(
                It.Is<BlockAnnouncement>(ba => ba.BlockHash == blockHeader.GetHash()),
                It.IsAny<Action<NetworkException>>()), Times.Once());
            _testContext.MockedPeers[2].Verify(p => p.EnqueueAnnouncement(
                It.Is<BlockAnnouncement>(ba => ba.BlockHash == blockHeader.GetHash()),
                It.IsAny<Action<NetworkException>>()), Times.Once());
        }

        [Fact]
        public async Task BroadcastTransaction_OnePeerKnowsTransaction_Test()
        {
            var transaction = OsCoreTestHelper.CreateFakeTransaction();

            _peerPool.GetPeers().First().TryAddKnownTransaction(transaction.GetHash());
            
            await _networkService.BroadcastTransactionAsync(transaction);

            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.TryAddKnownTransaction(transaction.GetHash()), Times.Once());

            _testContext.MockedPeers[0].Verify(p => p.EnqueueTransaction(It.Is<Transaction>(tx => tx.GetHash() == transaction.GetHash()),
                It.IsAny<Action<NetworkException>>()), Times.Never);
            _testContext.MockedPeers[1].Verify(p => p.EnqueueTransaction(It.Is<Transaction>(tx => tx.GetHash() == transaction.GetHash()),
                It.IsAny<Action<NetworkException>>()), Times.Once());
            _testContext.MockedPeers[2].Verify(p => p.EnqueueTransaction(It.Is<Transaction>(tx => tx.GetHash() == transaction.GetHash()),
                It.IsAny<Action<NetworkException>>()), Times.Once());
        }
    }
}