using System.Net;
using System.Threading.Tasks;
using AElf.Modularity;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS.Worker
{
    [DependsOn(typeof(OSTestAElfModule))]
    public class PeerReconnectionTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IAElfNetworkServer>(o =>
            {
                var service = new Mock<IAElfNetworkServer>();

                service.Setup(s => s.ConnectAsync(It.IsAny<DnsEndPoint>()))
                    .Returns<DnsEndPoint>(endpoint => Task.FromResult(endpoint.Port == 8001));

                return service.Object;
            });
        }
    }
}