using System.Threading.Tasks;
using AElf.Kernel.Node.Events;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using Moq;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.OS.Network
{
    public class SyncStateServiceTests : SyncFlagTestBase
    {
        private readonly ISyncStateService _syncStateService;
        private readonly IPeerPool _peerPool;
        private readonly ILocalEventBus _eventBus;

        public SyncStateServiceTests()
        {
            _syncStateService = GetRequiredService<ISyncStateService>();
            _peerPool = GetRequiredService<IPeerPool>();
            _eventBus = GetRequiredService<ILocalEventBus>();
        }
        
        [Fact]
        public async Task Initial_State_Is_Syncing()
        {
            _syncStateService.IsSyncFinished().ShouldBeFalse();
        }

        [Fact]
        public async Task Cannot_Retrigger_Sync()
        {
            await _syncStateService.UpdateSyncStateAsync();
            await _syncStateService.UpdateSyncStateAsync();
            
            _syncStateService.IsSyncFinished().ShouldBeTrue();
        }
        
        [Fact]
        public async Task Sync_Finished_Should_Launch_Event()
        {
            InitialSyncFinishedEvent eventData = null;
            _eventBus.Subscribe<InitialSyncFinishedEvent>(args =>
            {
                eventData = args;
                return Task.CompletedTask;
            });
            
            await _syncStateService.UpdateSyncStateAsync();
            
            eventData.ShouldNotBeNull();
        }
        
        [Fact]
        public async Task No_Peers_Stops_Sync()
        {
            await _syncStateService.UpdateSyncStateAsync();
            _syncStateService.IsSyncFinished().ShouldBeTrue();
        }
        
        [Fact]
        public async Task Peers_WithNoLib_Stops_Sync()
        {
            _peerPool.AddPeer(CreatePeer());
            _peerPool.AddPeer(CreatePeer());
            
            await _syncStateService.UpdateSyncStateAsync();
            
            _syncStateService.IsSyncFinished().ShouldBeTrue();
        }
        
        [Theory]
        [InlineData(true, new int[] {})]
        [InlineData(false, new []{15, 15})]
        [InlineData(true, new []{5, 15})]
        public async Task Trigger_Sync_Depends_On_Peers_And_Local_LIB(bool expectedSyncState, int[] peers)
        {
            foreach (int peer in peers)
                _peerPool.AddPeer(CreatePeer(peer));
            
            await _syncStateService.UpdateSyncStateAsync();
            
            _syncStateService.IsSyncFinished().ShouldBe(expectedSyncState);
        }

        private IPeer CreatePeer(long libHeight = 0)
        {
            Mock<IPeer> peerMock = new Mock<IPeer>();
            peerMock.Setup(p => p.LastKnowLibHeight).Returns(libHeight);
            return peerMock.Object;
        }
    }
}