using System.Net;
using System.Threading.Tasks;
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
            var handshakeProvider = services.GetServiceLazy<IHandshakeProvider>();

            services.AddTransient(provider =>
            {
                var mockService = new Mock<IConnectionService>();
                mockService.Setup(m => m.DoHandshakeAsync(It.IsAny<IPEndPoint>(), It.IsAny<Handshake>()))
                    .Returns(async () =>
                    {
                        var handshake = await handshakeProvider.Value.GetHandshakeAsync();

                        return new HandshakeReply
                        {
                            Error = HandshakeError.HandshakeOk,
                            Handshake = handshake
                        };
                    });

                return mockService.Object;
            });
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