using System.Collections.Generic;
using System.Linq;
using AElf.Modularity;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol.Types;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS.Network
{
    [DependsOn(typeof(OSCoreTestAElfModule))]
    public class PeerInvalidTransactionTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var peerPoolMock = new Mock<IPeerPool>();
            var peerList = new List<IPeer>
            {
                MockPeer("192.168.1.100:6800", "Peer1"),
                MockPeer("192.168.1.100:6801", "Peer2"),
                MockPeer("192.168.100.100:6800", "Peer3")
            };

            peerPoolMock.Setup(p => p.GetPeers(It.IsAny<bool>()))
                .Returns<bool>(includeFailing => peerList);
            peerPoolMock.Setup(p => p.GetPeersByHost(It.IsAny<string>()))
                .Returns<string>(host => { return peerList.Where(p => p.RemoteEndpoint.Host.Equals(host)).ToList(); });
            peerPoolMock.Setup(p => p.FindPeerByPublicKey(It.IsAny<string>()))
                .Returns<string>(pubkey => peerList.First(p => p.Info.Pubkey.Equals(pubkey)));
            
            context.Services.AddSingleton(o => peerPoolMock.Object);
        }

        private IPeer MockPeer(string address, string pubkey)
        {
            AElfPeerEndpointHelper.TryParse(address, out var endpoint);
            var peer = new Mock<IPeer>();
            var knowsTransactions = new HashSet<Hash>();
            for (var i = 0; i < 10; i++)
            {
                knowsTransactions.Add(HashHelper.ComputeFrom("Tx" + i + pubkey));
            }

            peer.Setup(p => p.KnowsTransaction(It.IsAny<Hash>()))
                .Returns<Hash>(hash => knowsTransactions.Contains(hash));
            peer.Setup(p => p.RemoteEndpoint).Returns(endpoint);
            peer.Setup(p => p.Info).Returns(new PeerConnectionInfo {Pubkey = pubkey});

            return peer.Object;
        }
    }
}