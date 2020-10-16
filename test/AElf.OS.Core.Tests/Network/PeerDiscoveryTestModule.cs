using System.Net;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Modularity;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol.Types;
using AElf.OS.Network.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.OS.Network
{
    [DependsOn(typeof(OSCoreTestAElfModule))]
    public class PeerDiscoveryTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IAElfNetworkServer>(o =>
            {
                var networkServer = new Mock<IAElfNetworkServer>();

                networkServer.Setup(s => s.CheckEndpointAvailableAsync(It.IsAny<DnsEndPoint>())).Returns<DnsEndPoint>(
                    endpoint =>
                    {
                        if (endpoint.Port == 8001)
                            return Task.FromResult(false);

                        return Task.FromResult(true);
                    });

                return networkServer.Object;
            });
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var peerPool = context.ServiceProvider.GetRequiredService<IPeerPool>();
            var accountService = context.ServiceProvider.GetRequiredService<IAccountService>();
            var pubkey = AsyncHelper.RunSync(accountService.GetPublicKeyAsync).ToHex();

            {
                var peerWithNoNode = new Mock<IPeer>();
                peerWithNoNode.Setup(p => p.Info).Returns(new PeerConnectionInfo
                    {Pubkey = "PeerWithNoNode", ConnectionTime = TimestampHelper.GetUtcNow()});
                peerWithNoNode.Setup(p => p.IsReady).Returns(true);
                peerWithNoNode.Setup(p => p.RemoteEndpoint).Returns(new AElfPeerEndpoint("192.168.88.100", 8801));
                peerWithNoNode.Setup(m => m.GetNodesAsync(It.IsAny<int>()))
                    .Returns(Task.FromResult(new NodeList()));

                peerPool.TryAddPeer(peerWithNoNode.Object);
            }

            {
                var peerWithUnavailableNode = new Mock<IPeer>();
                peerWithUnavailableNode.Setup(p => p.Info).Returns(new PeerConnectionInfo
                    {Pubkey = "PeerWithUnavailableNode", ConnectionTime = TimestampHelper.GetUtcNow()});
                peerWithUnavailableNode.Setup(p => p.IsReady).Returns(true);
                peerWithUnavailableNode.Setup(p => p.RemoteEndpoint)
                    .Returns(new AElfPeerEndpoint("192.168.88.100", 8802));
                peerWithUnavailableNode.Setup(m => m.GetNodesAsync(It.IsAny<int>()))
                    .Returns(Task.FromResult(new NodeList
                    {
                        Nodes =
                        {
                            new NodeInfo
                            {
                                Endpoint = "192.168.100.100:8001",
                                Pubkey = ByteString.CopyFromUtf8("192.168.100.100:8001")
                            }
                        }
                    }));

                peerPool.TryAddPeer(peerWithUnavailableNode.Object);
            }

            {
                var peerWittSamePubkeyNode = new Mock<IPeer>();
                peerWittSamePubkeyNode.Setup(p => p.Info).Returns(new PeerConnectionInfo
                    {Pubkey = "PeerWithSamePubkeyNode", ConnectionTime = TimestampHelper.GetUtcNow()});
                peerWittSamePubkeyNode.Setup(p => p.IsReady).Returns(true);
                peerWittSamePubkeyNode.Setup(p => p.RemoteEndpoint)
                    .Returns(new AElfPeerEndpoint("192.168.88.100", 8803));
                peerWittSamePubkeyNode.Setup(m => m.GetNodesAsync(It.IsAny<int>()))
                    .Returns(Task.FromResult(new NodeList
                    {
                        Nodes =
                        {
                            new NodeInfo
                            {
                                Endpoint = "192.168.100.100:8002",
                                Pubkey = ByteStringHelper.FromHexString(pubkey)
                            }
                        }
                    }));

                peerPool.TryAddPeer(peerWittSamePubkeyNode.Object);
            }

            {
                var peerWithNormalNode = new Mock<IPeer>();
                peerWithNormalNode.Setup(p => p.Info).Returns(new PeerConnectionInfo
                    {Pubkey = "PeerWithNormalNode", ConnectionTime = TimestampHelper.GetUtcNow()});
                peerWithNormalNode.Setup(p => p.IsReady).Returns(true);
                peerWithNormalNode.Setup(p => p.RemoteEndpoint).Returns(new AElfPeerEndpoint("192.168.88.100", 8804));
                peerWithNormalNode.Setup(m => m.GetNodesAsync(It.IsAny<int>()))
                    .Returns(Task.FromResult(new NodeList
                    {
                        Nodes =
                        {
                            new NodeInfo
                            {
                                Endpoint = "192.168.100.100:8003",
                                Pubkey = ByteString.CopyFromUtf8("192.168.100.100:8003")
                            }
                        }
                    }));

                peerPool.TryAddPeer(peerWithNormalNode.Object);
            }
        }
    }
}