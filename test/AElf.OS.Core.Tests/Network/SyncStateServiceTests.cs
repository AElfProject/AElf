using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel.Node.Events;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc;
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
            _syncStateService.SyncState.ShouldBe(SyncState.UnInitialized);
        }

        [Fact]
        public async Task Cannot_Retrigger_Sync()
        {
            await _syncStateService.StartSyncAsync();
            await _syncStateService.UpdateSyncStateAsync();
            
            _syncStateService.SyncState.ShouldBe(SyncState.Finished);
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
            
            await _syncStateService.StartSyncAsync();
            
            eventData.ShouldNotBeNull();
        }
        
        [Fact]
        public async Task No_Peers_Stops_Sync()
        {
            await _syncStateService.StartSyncAsync();
            _syncStateService.SyncState.ShouldBe(SyncState.Finished);
        }
        
        [Fact]
        public async Task Peers_WithNoLib_Stops_Sync()
        {
            _peerPool.TryAddPeer(CreatePeer());
            _peerPool.TryAddPeer(CreatePeer());
            
            await _syncStateService.StartSyncAsync();
            
            _syncStateService.SyncState.ShouldBe(SyncState.Finished);
        }
        
        [Theory]
        [InlineData(SyncState.Finished, new int[] {})]
        [InlineData(SyncState.Syncing, new []{15, 15})]
        [InlineData(SyncState.Finished, new []{5, 15})]
        public async Task Trigger_Sync_Depends_On_Peers_And_Local_LIB(SyncState expectedSyncState, int[] peers)
        {
            foreach (int peer in peers)
                _peerPool.TryAddPeer(CreatePeer(peer));
            
            await _syncStateService.StartSyncAsync();
            
            _syncStateService.SyncState.ShouldBe(expectedSyncState);
        }

        private IPeer CreatePeer(long libHeight = 0)
        {
            var peerMock = new Mock<IPeer>();
            
            peerMock.Setup(p => p.Info)
                .Returns(new PeerInfo {Pubkey = CryptoHelper.GenerateKeyPair().PublicKey.ToHex()});
            
            peerMock.Setup(p => p.LastKnownLibHeight).Returns(libHeight);
            
            return peerMock.Object;
        }
    }
}