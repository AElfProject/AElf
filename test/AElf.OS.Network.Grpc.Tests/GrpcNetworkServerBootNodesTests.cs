using System.Threading.Tasks;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.OS.Network
{
    public class GrpcNetworkServerBootNodesTests : GrpcNetworkWithBootNodesTestBase
    {
        private readonly IAElfNetworkServer _networkServer;
        private readonly ILocalEventBus _eventBus;


        public GrpcNetworkServerBootNodesTests()
        {
            _networkServer = GetRequiredService<IAElfNetworkServer>();
            _eventBus = GetRequiredService<ILocalEventBus>();
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

            await _networkServer.StopAsync();
        }
    }
}