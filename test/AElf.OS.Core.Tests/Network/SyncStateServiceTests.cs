using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Node.Events;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Grpc.Core;
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
            _syncStateService.IsSyncFinished.ShouldBeFalse();
        }

        [Fact]
        public async Task Cannot_Retrigger_Sync()
        {
            await _syncStateService.UpdateSyncStateAsync();
            await _syncStateService.UpdateSyncStateAsync();
            
            _syncStateService.IsSyncFinished.ShouldBeTrue();
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
            
            await _syncStateService.UpdateSyncStateAsync();
            
            eventData.ShouldNotBeNull();
        }
        
        [Fact]
        public async Task No_Peers_Stops_Sync()
        {
            await _syncStateService.UpdateSyncStateAsync();
            _syncStateService.IsSyncFinished.ShouldBeTrue();
        }
        
        [Fact]
        public async Task Peers_WithNoLib_Stops_Sync()
        {
            _peerPool.AddPeer(CreatePeer());
            _peerPool.AddPeer(CreatePeer());
            
            await _syncStateService.UpdateSyncStateAsync();
            
            _syncStateService.IsSyncFinished.ShouldBeTrue();
        }
        
        [Theory]
        [InlineData(true, new int[] {})]
        [InlineData(true, new []{15, 15})]
        [InlineData(true, new []{5, 15})]
        public async Task Peers_WithLib_LowerThanOffset_Sync(bool expectedSyncState, int[] peers)
        {
            foreach (int peer in peers)
                _peerPool.AddPeer(CreatePeer(peer));
            
            await _syncStateService.UpdateSyncStateAsync();
            
            _syncStateService.IsSyncFinished.ShouldBe(expectedSyncState);
        }
        
        [Fact]
        public async Task Peers_WithLib_Target_IsMin()
        {
            int minPeerLIB = 30;
            _peerPool.AddPeer(CreatePeer(minPeerLIB));
            _peerPool.AddPeer(CreatePeer(50));
            
            await _syncStateService.UpdateSyncStateAsync();
            
            _syncStateService.IsSyncFinished.ShouldBeFalse();
            _syncStateService.CurrentSyncTarget.Equals(minPeerLIB);
        }
        
        private IPeer CreatePeer(long libHeight = 0)
        {
            Mock<IPeer> peerMock = new Mock<IPeer>();
            peerMock.Setup(p => p.LastKnowLibHeight).Returns(libHeight);
            
//            var connectionInfo = new GrpcPeerInfo { LibHeightAtHandshake = libHeight };
//            var channel = new Channel("127.0.0.1:5000", ChannelCredentials.Insecure);
//            var client = new PeerService.PeerServiceClient(channel);
            return peerMock.Object;
        }
    }
}