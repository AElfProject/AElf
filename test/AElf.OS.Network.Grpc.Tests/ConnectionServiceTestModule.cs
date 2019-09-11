using System;
using System.Net;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Modularity;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Protocol;
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
                mockDialer.Setup(d => d.DialBackPeerAsync(It.IsAny<IPEndPoint>(), It.IsAny<Handshake>()))
                    .Returns<IPEndPoint, Handshake>((ip, _) =>
                    {
                        var randomKp = CryptoHelper.GenerateKeyPair();
                        return Task.FromResult(new GrpcPeer(
                                new GrpcClient(null, Mock.Of<PeerService.PeerServiceClient>()), ip, new PeerInfo
                                {
                                    Pubkey = randomKp.PublicKey.ToHex(),
                                    ConnectionTime = TimestampHelper.GetUtcNow()
                                }));
                    });

                return mockDialer.Object;
            });

            context.Services.AddSingleton<IHandshakeProvider>(sp =>
            {
                Mock<IHandshakeProvider> mockHskPro = new Mock<IHandshakeProvider>();
                mockHskPro.Setup(p => p.ValidateHandshakeAsync(It.IsAny<Handshake>()))
                    .Returns(Task.FromResult(HandshakeValidationResult.Ok));
                return mockHskPro.Object;
            });
        }
    }
}