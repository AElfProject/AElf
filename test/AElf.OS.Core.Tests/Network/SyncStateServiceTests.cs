using System.Threading.Tasks;
using AElf.Kernel.Node.Events;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Grpc.Core;
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
        public async Task Sync_Finished_Should_Launch_Event()
        {
            SyncFinishedEvent eventData = null;
            _eventBus.Subscribe<SyncFinishedEvent>(args =>
            {
                eventData = args;
                return Task.CompletedTask;
            });
            
            await _syncStateService.TryFindSyncTargetAsync();
            
            eventData.ShouldNotBeNull();
        }

        [Fact]
        public async Task Initial_State_Is_Syncing()
        {
            _syncStateService.IsSyncing.ShouldBeTrue();
        }
        
        [Fact]
        public async Task No_Peers_Stops_Sync()
        {
            await _syncStateService.TryFindSyncTargetAsync();
            _syncStateService.IsSyncing.ShouldBeFalse();
        }
        
        [Fact]
        public async Task Peers_WithNoLib_Stops_Sync()
        {
            _peerPool.AddPeer(CreatePeer());
            _peerPool.AddPeer(CreatePeer());
            
            await _syncStateService.TryFindSyncTargetAsync();
            
            _syncStateService.IsSyncing.ShouldBeFalse();
        }
        
        [Fact]
        public async Task Peers_WithLib_Target_IsMin()
        {
            int minPeerLIB = 30;
            _peerPool.AddPeer(CreatePeer(minPeerLIB));
            _peerPool.AddPeer(CreatePeer(50));
            
            await _syncStateService.TryFindSyncTargetAsync();
            
            _syncStateService.IsSyncing.ShouldBeTrue();
            _syncStateService.CurrentSyncTarget.Equals(minPeerLIB);
        }
        
        private GrpcPeer CreatePeer(long libHeight = 0)
        {
            var connectionInfo = new GrpcPeerInfo { LibHeightAtHandshake = libHeight };
            return new GrpcPeer(new Channel("127.0.0.1:5000", ChannelCredentials.Insecure), null, connectionInfo);
        }
    }
}