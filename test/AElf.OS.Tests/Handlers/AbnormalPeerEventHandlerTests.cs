using System.Threading.Tasks;
using AElf.OS.BlockSync.Events;
using AElf.OS.Network.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.OS.Handlers
{
    public class AbnormalPeerEventHandlerTests : AbnormalPeerEventHandlerTestBase
    {
        private readonly AbnormalPeerEventHandler _AbnormalPeerEventHandler;
        private readonly IBlackListedPeerProvider _blackListedPeerProvider;

        public AbnormalPeerEventHandlerTests()
        {
            _AbnormalPeerEventHandler = GetRequiredService<AbnormalPeerEventHandler>();
            _blackListedPeerProvider = GetRequiredService<IBlackListedPeerProvider>();
        }

        [Fact]
        public async Task HandleEvent_Test()
        {
            var peerHost = "192.168.100.200";
            _blackListedPeerProvider.IsIpBlackListed(peerHost).ShouldBeFalse();
            
            await _AbnormalPeerEventHandler.HandleEventAsync(new AbnormalPeerFoundEventData
            {
                BlockHash = HashHelper.ComputeFrom("Hash"),
                BlockHeight = 100,
                PeerPubkey = "NormalPeer"
            });
            
            _blackListedPeerProvider.IsIpBlackListed(peerHost).ShouldBeTrue();
        }
    }
}