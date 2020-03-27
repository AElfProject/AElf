using System.Threading.Tasks;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Application
{
    public class InvalidDataProcessingServiceTests : InvalidDataTestBase
    {
        private readonly IInvalidDataProcessingService _invalidDataProcessingService;
        private readonly IBlackListedPeerProvider _blackListedPeerProvider;
        private readonly IPeerInvalidDataProvider _peerInvalidDataProvider;
        private readonly IPeerPool _peerPool;

        public InvalidDataProcessingServiceTests()
        {
            _invalidDataProcessingService = GetRequiredService<IInvalidDataProcessingService>();
            _blackListedPeerProvider = GetRequiredService<IBlackListedPeerProvider>();
            _peerInvalidDataProvider = GetRequiredService<IPeerInvalidDataProvider>();
            _peerPool = GetRequiredService<IPeerPool>();
        }

        [Fact]
        public async Task ProcessInvalidTransaction_Test()
        {
            var peer1 = _peerPool.FindPeerByPublicKey("Peer1");
            var peer3 = _peerPool.FindPeerByPublicKey("Peer3");

            var txId = Hash.FromString("TxPeer3");
            bool isInBlackList;

            for (var i = 0; i < 5; i++)
            {
                await _invalidDataProcessingService.ProcessInvalidTransactionAsync(txId);
                isInBlackList = _blackListedPeerProvider.IsIpBlackListed(peer3.RemoteEndpoint.Host);
                isInBlackList.ShouldBeFalse();
            }

            await _invalidDataProcessingService.ProcessInvalidTransactionAsync(txId);
            isInBlackList = _blackListedPeerProvider.IsIpBlackListed(peer3.RemoteEndpoint.Host);
            isInBlackList.ShouldBeTrue();

            isInBlackList = _blackListedPeerProvider.IsIpBlackListed(peer1.RemoteEndpoint.Host);
            isInBlackList.ShouldBeFalse();
        }

        [Fact]
        public async Task ProcessInvalidTransaction_MultiplePorts_Test()
        {
            var peer1 = _peerPool.FindPeerByPublicKey("Peer1");
            var peer2 = _peerPool.FindPeerByPublicKey("Peer2");
            var peer3 = _peerPool.FindPeerByPublicKey("Peer3");

            bool isInBlackList;

            var txId = Hash.FromString("TxPeer1");
            for (var i = 0; i < 4; i++)
            {
                await _invalidDataProcessingService.ProcessInvalidTransactionAsync(txId);
                isInBlackList = _blackListedPeerProvider.IsIpBlackListed(peer1.RemoteEndpoint.Host);
                isInBlackList.ShouldBeFalse();
            }

            var txId2 = Hash.FromString("TxPeer2");
            await _invalidDataProcessingService.ProcessInvalidTransactionAsync(txId2);
            isInBlackList = _blackListedPeerProvider.IsIpBlackListed(peer2.RemoteEndpoint.Host);
            isInBlackList.ShouldBeFalse();

            await _invalidDataProcessingService.ProcessInvalidTransactionAsync(txId2);
            isInBlackList = _blackListedPeerProvider.IsIpBlackListed(peer1.RemoteEndpoint.Host);
            isInBlackList.ShouldBeTrue();
            isInBlackList = _blackListedPeerProvider.IsIpBlackListed(peer2.RemoteEndpoint.Host);
            isInBlackList.ShouldBeTrue();

            isInBlackList = _blackListedPeerProvider.IsIpBlackListed(peer3.RemoteEndpoint.Host);
            isInBlackList.ShouldBeFalse();
        }
    }
}