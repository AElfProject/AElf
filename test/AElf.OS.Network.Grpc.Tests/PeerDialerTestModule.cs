using System.Net;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS.Network.Grpc;

[DependsOn(
    typeof(GrpcNetworkWithChainTestModule))]
public class PeerDialerTestModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        services.AddTransient(provider =>
        {
            var mockService = new Mock<IConnectionService>();
            mockService.Setup(m => m.DoHandshakeAsync(It.IsAny<DnsEndPoint>(), It.IsAny<Handshake>()))
                .Returns<DnsEndPoint, Handshake>((pe, hsk) =>
                {
                    var handshake = NetworkTestHelper.CreateValidHandshake(CryptoHelper.GenerateKeyPair(), 10,
                        hsk.HandshakeData.ChainId);

                    var handShakeReply = Task.FromResult(new HandshakeReply
                    {
                        Error = HandshakeError.HandshakeOk,
                        Handshake = handshake
                    });
                    return handShakeReply;
                });

            return mockService.Object;
        });
    }
}

[DependsOn(
    typeof(GrpcNetworkWithChainTestModule))]
public class PeerDialerInvalidHandshakeTestModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        services.AddTransient(provider =>
        {
            var mockService = new Mock<IConnectionService>();
            mockService.Setup(m => m.DoHandshakeAsync(It.IsAny<DnsEndPoint>(), It.IsAny<Handshake>()))
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
    typeof(GrpcNetworkWithChainTestModule))]
public class PeerDialerReplyErrorTestModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        services.AddTransient(provider =>
        {
            var mockService = new Mock<IConnectionService>();
            mockService.Setup(m => m.DoHandshakeAsync(It.IsAny<DnsEndPoint>(), It.IsAny<Handshake>()))
                .Returns(Task.FromResult(new HandshakeReply
                {
                    Error = HandshakeError.ChainMismatch
                }));

            return mockService.Object;
        });
    }
}