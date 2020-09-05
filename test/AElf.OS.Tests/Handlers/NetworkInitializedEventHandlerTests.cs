using System.Threading.Tasks;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.OS.Handlers
{
    public class NetworkInitializedEventHandlerTests : OSTestBase
    {
        private readonly NetworkInitializedEventHandler _networkInitializedEventHandler;
        private readonly INodeSyncStateProvider _syncStateProvider;

        public NetworkInitializedEventHandlerTests()
        {
            _networkInitializedEventHandler = GetRequiredService<NetworkInitializedEventHandler>();
            _syncStateProvider = GetRequiredService<INodeSyncStateProvider>();
        }

        [Fact]
        public async Task HandleEvent_Test()
        {
            await _networkInitializedEventHandler.HandleEventAsync(new NetworkInitializedEvent());
            _syncStateProvider.SyncTarget.ShouldBe(-1);
        }
    }
}