using System;
using System.Threading.Tasks;
using AElf.OS.Helpers;
using AElf.OS.Network.Application;
using AElf.Types;
using Moq;
using Xunit;

namespace AElf.OS.Network.Service
{
    public class NetworkServicePropagationTest : NetworkServicePropagationTestBase
    {
        private NetworkServicePropagationTestContext _testContext;
        private INetworkService _networkService;

        public NetworkServicePropagationTest()
        {
            _testContext = GetRequiredService<NetworkServicePropagationTestContext>();
            _networkService = GetRequiredService<INetworkService>();
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
    }
}