using AElf.Kernel;
using AElf.OS.Network.Infrastructure;
using AElf.Sdk.CSharp;
using Shouldly;
using Xunit;

namespace AElf.OS.Network
{
    public class PeerReconnectionTests : NetworkInfrastructureTestBase
    {
        private readonly IPeerReconnectionStateProvider _reconnectionProvider;

        public PeerReconnectionTests(IPeerReconnectionStateProvider reconnectionProvider)
        {
            _reconnectionProvider = reconnectionProvider;
        }
        
        [Fact]
        public void AddedPeer_IsFindable_ByAddressAndPubkey()
        {
            int period = 5_000;

            var deconnectionPeriod = TimestampHelper.GetUtcNow();

            _reconnectionProvider.AddReconnectingPeer("ipport",
                new ReconnectingPeer {Endpoint = "ipport", NextAttempt = deconnectionPeriod });


            var nowAfter = deconnectionPeriod.AddMilliseconds(2 * period);

            var r = _reconnectionProvider.GetPeersReadyForReconnection(nowAfter);
            
            r.Count.ShouldBe(1);
        }
    }
}