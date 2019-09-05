using System.Net;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Modularity;
using AElf.OS.Network.Grpc;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS.Network
{
    [DependsOn(typeof(OSCoreTestAElfModule), typeof(GrpcNetworkModule))]
    public class ConnectionServiceTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<NetworkOptions>(o => { o.MaxPeersPerIpAddress = 1; });

            context.Services.AddSingleton(sp =>
            {
                Mock<IPeerDialer> mockDialer = new Mock<IPeerDialer>();
                mockDialer.Setup(d => d.DialBackPeer(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionInfo>()))
                    .Returns<IPEndPoint, ConnectionInfo>((ip, _) =>
                    {
                        var randomKp = CryptoHelper.GenerateKeyPair();
                        return Task.FromResult(new GrpcPeer(
                                new GrpcClient(null, Mock.Of<PeerService.PeerServiceClient>()), ip, new PeerInfo
                                {
                                    Pubkey = randomKp.PublicKey.ToHex()
                                }));
                    });

                return mockDialer.Object;
            });
        }
    }
}