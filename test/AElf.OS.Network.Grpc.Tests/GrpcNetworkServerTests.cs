using System.Threading.Tasks;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.OS.Network
{
    public class GrpcNetworkServerTests : GrpcNetworkTestBase
    {
        private readonly IAElfNetworkServer _networkServer;
        private readonly ILocalEventBus _eventBus;

        public GrpcNetworkServerTests()
        {
            _networkServer = GetRequiredService<IAElfNetworkServer>();;
            _eventBus = GetRequiredService<ILocalEventBus>();;
        }
        
        [Fact]
        public async Task Start_Should_Launch_Net_Init_Event()
        {
            NetworkInitializationFinishedEvent eventData = null;
            _eventBus.Subscribe<NetworkInitializationFinishedEvent>(ed =>
            {
                eventData = ed; 
                return Task.CompletedTask;
            });
            
            await _networkServer.StartAsync();
            await _networkServer.StopAsync();
            
            eventData.ShouldNotBeNull();
        }
    }
}