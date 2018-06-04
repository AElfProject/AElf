using System.Threading.Tasks;
using AElf.Network.Peers;
using Moq;
using Xunit;

namespace AElf.Network.Tests.PeerManagement
{
    public class PeerMaintenanceUnitTests
    {
        [Fact]
        public async Task DoPeerMaintenance_ShouldDropBootnode_AfterThreshold()
        {
            PeerManager peerManager = new PeerManager(null, null, null, null);

            for (int i = 0; i < peerManager.BootnodeDropThreshold; i++)
            {
                peerManager.AddPeer(CreateMockPeer());
            }

            var bootnode = CreateMockPeer(true);
            peerManager.AddPeer(bootnode);
            
            Assert.NotNull(peerManager.GetPeer(bootnode)); // maybe not necessary
            
            peerManager.DoPeerMaintenance();
            
            Assert.Null(peerManager.GetPeer(bootnode));
        }

        private IPeer CreateMockPeer(bool isBootnode = false)
        {
            Mock<IPeer> mock = new Mock<IPeer>();
            mock.Setup(m => m.IsBootnode).Returns(isBootnode);
            mock.Setup(m => m.StartListeningAsync()).Returns(Task.FromResult(true));

            return mock.Object;
        }
    }
}