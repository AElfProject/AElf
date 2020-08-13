using System.Linq;
using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.OS.Network.Grpc
{
    public class GrpcNetworkServerBootNodesTests : GrpcNetworkWithBootNodesTestBase
    {
        private readonly IAElfNetworkServer _networkServer;
        private readonly ILocalEventBus _eventBus;
        private readonly IReconnectionService _reconnectionService;

        public GrpcNetworkServerBootNodesTests()
        {
            _networkServer = GetRequiredService<IAElfNetworkServer>();
            _eventBus = GetRequiredService<ILocalEventBus>();
            _reconnectionService = GetRequiredService<IReconnectionService>();
        }

        [Fact]
        public async Task StartServer_Test()
        {
            NetworkInitializedEvent received = null;
            _eventBus.Subscribe<NetworkInitializedEvent>(a =>
            {
                received = a;
                return Task.CompletedTask;
            });

            await _networkServer.StartAsync();

            received.ShouldNotBeNull();

            var reconnections = _reconnectionService.GetPeersReadyForReconnection(null);
            reconnections.Count.ShouldBe(1);
            reconnections.First().Endpoint.ShouldBe("127.0.0.1:2018");

            await _networkServer.StopAsync();
        }
    }
}