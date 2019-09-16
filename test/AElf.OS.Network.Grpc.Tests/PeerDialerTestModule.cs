using System.Net;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Modularity;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.OS.Network
{
    [DependsOn(
        typeof(GrpcNetworkTestModule))]
    public class PeerDialerTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            var netTestContext = new NetworkTestContext();

            services.AddTransient(provider =>
            {
                var mockService = new Mock<IConnectionService>();
                mockService.Setup(m => m.DoHandshakeAsync(It.IsAny<IPEndPoint>(), It.IsAny<Handshake>()))
                    .Returns<IPEndPoint, Handshake>(async (pe, hsk) =>
                    {
                        var handshake = NetworkTestHelper.CreateValidHandshake(CryptoHelper.GenerateKeyPair(), 10, hsk.HandshakeData.ChainId);
                        netTestContext.GeneratedHandshakes[pe.Address.ToString()] = handshake;

                        return new HandshakeReply
                        {
                            Error = HandshakeError.HandshakeOk,
                            Handshake = handshake
                        };
                    });

                return mockService.Object;
            });

            services.AddSingleton<NetworkTestContext>(netTestContext);
        }
    }

    [DependsOn(
        typeof(GrpcNetworkTestModule))]
    public class PeerDialerInvalidHandshakeTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddTransient(provider =>
            {
                var mockService = new Mock<IConnectionService>();
                mockService.Setup(m=>m.DoHandshakeAsync(It.IsAny<IPEndPoint>(), It.IsAny<Handshake>()))
                    .Returns(Task.FromResult(new HandshakeReply
                    {
                        Error = HandshakeError.HandshakeOk,
                        Handshake = new Handshake
                        {
                            HandshakeData = new HandshakeData()
                        }
                    }));

                return mockService.Object;
            });
        }
    }
    
    [DependsOn(
        typeof(GrpcNetworkTestModule))]
    public class PeerDialerReplyErrorTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddTransient(provider =>
            {
                var mockService = new Mock<IConnectionService>();
                mockService.Setup(m=>m.DoHandshakeAsync(It.IsAny<IPEndPoint>(), It.IsAny<Handshake>()))
                    .Returns(Task.FromResult(new HandshakeReply
                    {
                        Error = HandshakeError.ChainMismatch
                    }));

                return mockService.Object;
            });
        }
    }
}