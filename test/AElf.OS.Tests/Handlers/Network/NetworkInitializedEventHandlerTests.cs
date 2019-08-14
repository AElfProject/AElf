using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using Shouldly;
using Xunit;

namespace AElf.OS.Handlers.Network
{
    public class NetworkInitializedEventHandlerTests : BlockSyncTestBase
    {
        private readonly NetworkInitializedEventHandler _networkInitializedEventHandler;
        private readonly ISyncStateService _syncStateService;

        public NetworkInitializedEventHandlerTests()
        {
            _networkInitializedEventHandler = GetRequiredService<NetworkInitializedEventHandler>();
            _syncStateService = GetRequiredService<ISyncStateService>();
        }

        [Fact]
        public async Task InitializeNetwork_Test()
        {
            var beforeState = _syncStateService.SyncState;
            beforeState.ShouldBe(SyncState.UnInitialized);

            await _networkInitializedEventHandler.HandleEventAsync(new NetworkInitializedEvent());

            var afterState = _syncStateService.SyncState;
            afterState.ShouldBe(SyncState.Finished);
        }
    }
}