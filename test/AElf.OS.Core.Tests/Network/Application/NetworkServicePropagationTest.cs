using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Helpers;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Moq;
using Xunit;

namespace AElf.OS.Network.Application
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
        public async Task BroadcastBlock_OldBlock_Test()
        {
            var blockHeader = OsCoreTestHelper.CreateFakeBlockHeader(chainId: 1, height: 2);
            blockHeader.Time = TimestampHelper.GetUtcNow() -
                               TimestampHelper.DurationFromMinutes(
                                   NetworkConstants.DefaultMaxBlockAgeToBroadcastInMinutes + 1);
            var blockWithTx = new BlockWithTransactions { Header = blockHeader };

            await _networkService.BroadcastBlockWithTransactionsAsync(blockWithTx);
            
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.TryAddKnownBlock(blockHeader.GetHash()), Times.Never);
            
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueBlock(blockWithTx, It.IsAny<Action<NetworkException>>()), Times.Never);
        }

        [Fact]
        public async Task BroadcastBlock_Test()
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
        public async Task BroadcastBlock_ThrowNetworkException_Test()
        {
            _testContext.MockedPeers[0]
                .Setup(p => p.EnqueueBlock(It.IsAny<BlockWithTransactions>(), It.IsAny<Action<NetworkException>>()))
                .Callback<BlockWithTransactions, Action<NetworkException>>((block, action) =>
                    action.Invoke(new NetworkException()));
            _testContext.MockedPeers[1]
                .Setup(p => p.EnqueueBlock(It.IsAny<BlockWithTransactions>(), It.IsAny<Action<NetworkException>>()))
                .Callback<BlockWithTransactions, Action<NetworkException>>((block, action) =>
                    action.Invoke(new NetworkException("Unrecoverable", NetworkExceptionType.Unrecoverable)));
            _testContext.MockedPeers[2]
                .Setup(p => p.EnqueueBlock(It.IsAny<BlockWithTransactions>(), It.IsAny<Action<NetworkException>>()))
                .Callback<BlockWithTransactions, Action<NetworkException>>((block, action) =>
                    action.Invoke(new NetworkException("PeerUnstable", NetworkExceptionType.PeerUnstable)));

            var blockHeader = OsCoreTestHelper.CreateFakeBlockHeader(chainId: 1, height: 2);
            var blockWithTx = new BlockWithTransactions {Header = blockHeader};

            await _networkService.BroadcastBlockWithTransactionsAsync(blockWithTx);

            foreach (var peer in _testContext.MockedPeers)
            {
                peer.Verify(p => p.EnqueueBlock(blockWithTx, It.IsAny<Action<NetworkException>>()), Times.Once());
                peer.Verify(p => p.TryAddKnownBlock(blockHeader.GetHash()), Times.Once());
            }

            _testContext.MockAElfNetworkServer.Verify(s => s.TrySchedulePeerReconnectionAsync(It.IsAny<IPeer>()),
                Times.Once());
        }

        [Fact]
        public async Task BroadcastBlock_PeerNotReady_Test()
        {
            _testContext.MockedPeers[0]
                .Setup(p => p.EnqueueBlock(It.IsAny<BlockWithTransactions>(), It.IsAny<Action<NetworkException>>()))
                .Throws<NetworkException>();

            var blockHeader = OsCoreTestHelper.CreateFakeBlockHeader(chainId: 1, height: 2);
            var blockWithTx = new BlockWithTransactions {Header = blockHeader};

            await _networkService.BroadcastBlockWithTransactionsAsync(blockWithTx);
            
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueBlock(blockWithTx, It.IsAny<Action<NetworkException>>()), Times.Once());

            _testContext.MockedPeers[0].Verify(p => p.TryAddKnownBlock(blockHeader.GetHash()), Times.Never);
            _testContext.MockedPeers[1].Verify(p => p.TryAddKnownBlock(blockHeader.GetHash()), Times.Once());
            _testContext.MockedPeers[2].Verify(p => p.TryAddKnownBlock(blockHeader.GetHash()), Times.Once());
        }

        [Fact]
        public async Task BroadcastAnnouncement_OldBlock_Test()
        {
            var blockHeader = OsCoreTestHelper.CreateFakeBlockHeader(chainId: 1, height: 2);
            blockHeader.Time = TimestampHelper.GetUtcNow() -
                               TimestampHelper.DurationFromMinutes(
                                   NetworkConstants.DefaultMaxBlockAgeToBroadcastInMinutes + 1);

            await _networkService.BroadcastAnnounceAsync(blockHeader);
            
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.TryAddKnownBlock(blockHeader.GetHash()), Times.Never);
            
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueAnnouncement(It.Is<BlockAnnouncement>(ba => ba.BlockHash == blockHeader.GetHash()), 
                    It.IsAny<Action<NetworkException>>()), Times.Never);
        }
        
        [Fact]
        public async Task BroadcastAnnouncement_Test()
        {
            var blockHeader = OsCoreTestHelper.CreateFakeBlockHeader(chainId: 1, height: 2);

            await _networkService.BroadcastAnnounceAsync(blockHeader);
            
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueAnnouncement(It.Is<BlockAnnouncement>(ba => ba.BlockHash == blockHeader.GetHash()), 
                    It.IsAny<Action<NetworkException>>()), Times.Once());
            
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.TryAddKnownBlock(blockHeader.GetHash()), Times.Once());
            
            await _networkService.BroadcastAnnounceAsync(blockHeader);
            
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueAnnouncement(It.Is<BlockAnnouncement>(ba => ba.BlockHash == blockHeader.GetHash()), 
                    It.IsAny<Action<NetworkException>>()), Times.Once());
        }
        
        [Fact]
        public async Task BroadcastAnnouncement_ThrowNetworkException_Test()
        {
            _testContext.MockedPeers[0]
                .Setup(p => p.EnqueueAnnouncement(It.IsAny<BlockAnnouncement>(), It.IsAny<Action<NetworkException>>()))
                .Callback<BlockAnnouncement, Action<NetworkException>>((block, action) =>
                    action.Invoke(new NetworkException()));
            _testContext.MockedPeers[1]
                .Setup(p => p.EnqueueAnnouncement(It.IsAny<BlockAnnouncement>(), It.IsAny<Action<NetworkException>>()))
                .Callback<BlockAnnouncement, Action<NetworkException>>((block, action) =>
                    action.Invoke(new NetworkException("Unrecoverable", NetworkExceptionType.Unrecoverable)));
            _testContext.MockedPeers[2]
                .Setup(p => p.EnqueueAnnouncement(It.IsAny<BlockAnnouncement>(), It.IsAny<Action<NetworkException>>()))
                .Callback<BlockAnnouncement, Action<NetworkException>>((block, action) =>
                    action.Invoke(new NetworkException("PeerUnstable", NetworkExceptionType.PeerUnstable)));

            var blockHeader = OsCoreTestHelper.CreateFakeBlockHeader(chainId: 1, height: 2);
            
            await _networkService.BroadcastAnnounceAsync(blockHeader);

            foreach (var peer in _testContext.MockedPeers)
            {
                peer.Verify(p => p.EnqueueAnnouncement(It.Is<BlockAnnouncement>(ba => ba.BlockHash == blockHeader.GetHash()), 
                    It.IsAny<Action<NetworkException>>()), Times.Once());
                peer.Verify(p => p.TryAddKnownBlock(blockHeader.GetHash()), Times.Once());
                
            }

            _testContext.MockAElfNetworkServer.Verify(s => s.TrySchedulePeerReconnectionAsync(It.IsAny<IPeer>()),
                Times.Once());
        }

        [Fact]
        public async Task BroadcastAnnouncement_PeerNotReady_Test()
        {
            _testContext.MockedPeers[0]
                .Setup(p => p.EnqueueAnnouncement(It.IsAny<BlockAnnouncement>(), It.IsAny<Action<NetworkException>>()))
                .Throws<NetworkException>();

            var blockHeader = OsCoreTestHelper.CreateFakeBlockHeader(chainId: 1, height: 2);

            await _networkService.BroadcastAnnounceAsync(blockHeader);
            
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueAnnouncement(It.Is<BlockAnnouncement>(ba => ba.BlockHash == blockHeader.GetHash()), 
                    It.IsAny<Action<NetworkException>>()), Times.Once());
            
            _testContext.MockedPeers[0].Verify(p => p.TryAddKnownBlock(blockHeader.GetHash()), Times.Never);
            _testContext.MockedPeers[1].Verify(p => p.TryAddKnownBlock(blockHeader.GetHash()), Times.Once());
            _testContext.MockedPeers[2].Verify(p => p.TryAddKnownBlock(blockHeader.GetHash()), Times.Once());
        }

        [Fact]
        public async Task BroadcastTransaction_Test()
        {
            var transaction = OsCoreTestHelper.CreateFakeTransaction();
            
            await _networkService.BroadcastTransactionAsync(transaction);
            
            foreach (var peer in _testContext.MockedPeers)
                peer.Object.TryAddKnownTransaction(transaction.GetHash());
            
            await _networkService.BroadcastTransactionAsync(transaction);
            
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueTransaction(It.Is<Transaction>(tx => tx.GetHash() == transaction.GetHash()),
                    It.IsAny<Action<NetworkException>>()), Times.Once());
        }
        
        [Fact]
        public async Task BroadcastTransaction_ThrowNetworkException_Test()
        {
            _testContext.MockedPeers[0]
                .Setup(p => p.EnqueueTransaction(It.IsAny<Transaction>(), It.IsAny<Action<NetworkException>>()))
                .Callback<Transaction, Action<NetworkException>>((block, action) =>
                    action.Invoke(new NetworkException()));
            _testContext.MockedPeers[1]
                .Setup(p => p.EnqueueTransaction(It.IsAny<Transaction>(), It.IsAny<Action<NetworkException>>()))
                .Callback<Transaction, Action<NetworkException>>((block, action) =>
                    action.Invoke(new NetworkException("Unrecoverable", NetworkExceptionType.Unrecoverable)));
            _testContext.MockedPeers[2]
                .Setup(p => p.EnqueueTransaction(It.IsAny<Transaction>(), It.IsAny<Action<NetworkException>>()))
                .Callback<Transaction, Action<NetworkException>>((block, action) =>
                    action.Invoke(new NetworkException("PeerUnstable", NetworkExceptionType.PeerUnstable)));

            var transaction = OsCoreTestHelper.CreateFakeTransaction();
            
            await _networkService.BroadcastTransactionAsync(transaction);

            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueTransaction(It.Is<Transaction>(tx => tx.GetHash() == transaction.GetHash()),
                    It.IsAny<Action<NetworkException>>()), Times.Once());
            
            _testContext.MockAElfNetworkServer.Verify(s => s.TrySchedulePeerReconnectionAsync(It.IsAny<IPeer>()),
                Times.Once());
        }
        
        [Fact]
        public async Task BroadcastTransaction_PeerNotReady_Test()
        {
            _testContext.MockedPeers[0]
                .Setup(p => p.EnqueueTransaction(It.IsAny<Transaction>(), It.IsAny<Action<NetworkException>>()))
                .Throws<NetworkException>();

            var transaction = OsCoreTestHelper.CreateFakeTransaction();
            
            await _networkService.BroadcastTransactionAsync(transaction);

            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueTransaction(It.Is<Transaction>(tx => tx.GetHash() == transaction.GetHash()),
                    It.IsAny<Action<NetworkException>>()), Times.Once());
        }

        [Fact]
        public async Task BroadcastLibAnnouncement_Test()
        {
            var libHash = HashHelper.ComputeFrom("LibHash");
            var libHeight = 2;

            await _networkService.BroadcastLibAnnounceAsync(libHash, libHeight);

            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueLibAnnouncement(
                    It.Is<LibAnnouncement>(a => a.LibHash == libHash && a.LibHeight == libHeight),
                    It.IsAny<Action<NetworkException>>()), Times.Once());
        }
        
        [Fact]
        public async Task BroadcastLibAnnouncement_ThrowNetworkException_Test()
        {
            _testContext.MockedPeers[0]
                .Setup(p => p.EnqueueLibAnnouncement(It.IsAny<LibAnnouncement>(), It.IsAny<Action<NetworkException>>()))
                .Callback<LibAnnouncement, Action<NetworkException>>((block, action) =>
                    action.Invoke(new NetworkException()));
            _testContext.MockedPeers[1]
                .Setup(p => p.EnqueueLibAnnouncement(It.IsAny<LibAnnouncement>(), It.IsAny<Action<NetworkException>>()))
                .Callback<LibAnnouncement, Action<NetworkException>>((block, action) =>
                    action.Invoke(new NetworkException("Unrecoverable", NetworkExceptionType.Unrecoverable)));
            _testContext.MockedPeers[2]
                .Setup(p => p.EnqueueLibAnnouncement(It.IsAny<LibAnnouncement>(), It.IsAny<Action<NetworkException>>()))
                .Callback<LibAnnouncement, Action<NetworkException>>((block, action) =>
                    action.Invoke(new NetworkException("PeerUnstable", NetworkExceptionType.PeerUnstable)));

            var libHash = HashHelper.ComputeFrom("LibHash");
            var libHeight = 2;
            
            await _networkService.BroadcastLibAnnounceAsync(libHash, libHeight);

            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueLibAnnouncement(
                    It.Is<LibAnnouncement>(a => a.LibHash == libHash && a.LibHeight == libHeight),
                    It.IsAny<Action<NetworkException>>()), Times.Once());
            
            _testContext.MockAElfNetworkServer.Verify(s => s.TrySchedulePeerReconnectionAsync(It.IsAny<IPeer>()),
                Times.Once());
        }
        
        [Fact]
        public async Task BroadcastLibAnnouncement_PeerNotReady_Test()
        {
            var libHash = HashHelper.ComputeFrom("LibHash");
            var libHeight = 2;
            
            _testContext.MockedPeers[0]
                .Setup(p => p.EnqueueLibAnnouncement(It.IsAny<LibAnnouncement>(), It.IsAny<Action<NetworkException>>()))
                .Throws<NetworkException>();
            
            await _networkService.BroadcastLibAnnounceAsync(libHash, libHeight);

            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueLibAnnouncement(
                    It.Is<LibAnnouncement>(a => a.LibHash == libHash && a.LibHeight == libHeight),
                    It.IsAny<Action<NetworkException>>()), Times.Once());
        }

        [Fact]
        public async Task BroadcastBlock_OnePeerKnowsBlock_Test()
        {
            var blockHeader = OsCoreTestHelper.CreateFakeBlockHeader(1, 2);
            var blockWithTransactions = new BlockWithTransactions {Header = blockHeader};

            _peerPool.FindPeerByPublicKey("Pubkey0").TryAddKnownBlock(blockWithTransactions.GetHash());

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

            _peerPool.FindPeerByPublicKey("Pubkey0").TryAddKnownBlock(blockHeader.GetHash());

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

            _peerPool.FindPeerByPublicKey("Pubkey0").TryAddKnownTransaction(transaction.GetHash());
            
            await _networkService.BroadcastTransactionAsync(transaction);

            _testContext.MockedPeers[0].Verify(p => p.EnqueueTransaction(It.Is<Transaction>(tx => tx.GetHash() == transaction.GetHash()),
                It.IsAny<Action<NetworkException>>()), Times.Never);
            _testContext.MockedPeers[1].Verify(p => p.EnqueueTransaction(It.Is<Transaction>(tx => tx.GetHash() == transaction.GetHash()),
                It.IsAny<Action<NetworkException>>()), Times.Once());
            _testContext.MockedPeers[2].Verify(p => p.EnqueueTransaction(It.Is<Transaction>(tx => tx.GetHash() == transaction.GetHash()),
                It.IsAny<Action<NetworkException>>()), Times.Once());
        }
    }
}