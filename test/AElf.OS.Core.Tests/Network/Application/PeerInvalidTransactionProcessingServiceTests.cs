using System.Threading.Tasks;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Application
{
    public class PeerPeerInvalidTransactionProcessingServiceTests : PeerInvalidTransactionTestBase
    {
        private readonly IPeerInvalidTransactionProcessingService _peerInvalidTransactionProcessingService;
        private readonly IBlackListedPeerProvider _blackListedPeerProvider;
        private readonly IPeerInvalidTransactionProvider _peerInvalidTransactionProvider;
        private readonly IPeerPool _peerPool;

        public PeerPeerInvalidTransactionProcessingServiceTests()
        {
            _peerInvalidTransactionProcessingService = GetRequiredService<IPeerInvalidTransactionProcessingService>();
            _blackListedPeerProvider = GetRequiredService<IBlackListedPeerProvider>();
            _peerInvalidTransactionProvider = GetRequiredService<IPeerInvalidTransactionProvider>();
            _peerPool = GetRequiredService<IPeerPool>();
        }

        [Fact]
        public async Task ProcessPeerInvalidTransaction_Test()
        {
            var peer1 = _peerPool.FindPeerByPublicKey("Peer1");
            var peer3 = _peerPool.FindPeerByPublicKey("Peer3");
            
            bool isInBlackList;

            for (var i = 0; i < 5; i++)
            {
                var txId = HashHelper.ComputeFrom("Tx" + i + "Peer3");
                await _peerInvalidTransactionProcessingService.ProcessPeerInvalidTransactionAsync(txId);
                isInBlackList = _blackListedPeerProvider.IsIpBlackListed(peer3.RemoteEndpoint.Host);
                isInBlackList.ShouldBeFalse();
            }

            await _peerInvalidTransactionProcessingService.ProcessPeerInvalidTransactionAsync(HashHelper.ComputeFrom("Tx" + 5 + "Peer3"));
            isInBlackList = _blackListedPeerProvider.IsIpBlackListed(peer3.RemoteEndpoint.Host);
            isInBlackList.ShouldBeTrue();

            isInBlackList = _blackListedPeerProvider.IsIpBlackListed(peer1.RemoteEndpoint.Host);
            isInBlackList.ShouldBeFalse();
        }

        [Fact]
        public async Task ProcessPeerInvalidTransaction_MultiplePorts_Test()
        {
            var peer1 = _peerPool.FindPeerByPublicKey("Peer1");
            var peer2 = _peerPool.FindPeerByPublicKey("Peer2");
            var peer3 = _peerPool.FindPeerByPublicKey("Peer3");
            
            bool isInBlackList;

            for (var i = 0; i < 4; i++)
            {
                var txId = HashHelper.ComputeFrom("Tx" + i + "Peer1");
                await _peerInvalidTransactionProcessingService.ProcessPeerInvalidTransactionAsync(txId);
                isInBlackList = _blackListedPeerProvider.IsIpBlackListed(peer1.RemoteEndpoint.Host);
                isInBlackList.ShouldBeFalse();
            }

            await _peerInvalidTransactionProcessingService.ProcessPeerInvalidTransactionAsync(HashHelper.ComputeFrom("Tx0Peer2"));
            isInBlackList = _blackListedPeerProvider.IsIpBlackListed(peer2.RemoteEndpoint.Host);
            isInBlackList.ShouldBeFalse();

            await _peerInvalidTransactionProcessingService.ProcessPeerInvalidTransactionAsync(HashHelper.ComputeFrom("Tx1Peer2"));
            isInBlackList = _blackListedPeerProvider.IsIpBlackListed(peer1.RemoteEndpoint.Host);
            isInBlackList.ShouldBeTrue();
            isInBlackList = _blackListedPeerProvider.IsIpBlackListed(peer2.RemoteEndpoint.Host);
            isInBlackList.ShouldBeTrue();

            isInBlackList = _blackListedPeerProvider.IsIpBlackListed(peer3.RemoteEndpoint.Host);
            isInBlackList.ShouldBeFalse();
        }

        [Fact]
        public async Task ProcessPeerInvalidTransaction_RepeatedTransaction_Test()
        {
            var peer1 = _peerPool.FindPeerByPublicKey("Peer1");
            
            var txId = HashHelper.ComputeFrom("Tx0Peer1");
            for (var i = 0; i < 6; i++)
            {
                await _peerInvalidTransactionProcessingService.ProcessPeerInvalidTransactionAsync(txId);
            }
            
            var isInBlackList = _blackListedPeerProvider.IsIpBlackListed(peer1.RemoteEndpoint.Host);
            isInBlackList.ShouldBeFalse();
        }
    }
}