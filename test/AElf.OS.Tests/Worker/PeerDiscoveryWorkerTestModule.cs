using System.Net;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Modularity;
using AElf.OS.Network;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS.Worker
{
    [DependsOn(typeof(OSTestAElfModule),
        typeof(PeerDiscoveryTestModule))]
    public class PeerDiscoveryWorkerTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var peerPool = context.Services.GetRequiredServiceLazy<IPeerPool>();
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

                networkServer.Setup(s => s.ConnectAsync(It.IsAny<DnsEndPoint>())).Returns<DnsEndPoint>(
                    endpoint =>
                    {
                        var peer = new Mock<IPeer>();
                        peer.Setup(p => p.IsReady).Returns(true);
                        peer.Setup(p => p.Info).Returns(new PeerConnectionInfo
                            {Pubkey = endpoint.ToString(), ConnectionTime = TimestampHelper.GetUtcNow()});
                        peer.Setup(p => p.RemoteEndpoint).Returns(endpoint);

                        peerPool.Value.TryAddPeer(peer.Object);
                        return Task.FromResult(true);
                    });

                return networkServer.Object;
            });
        }
    }
}