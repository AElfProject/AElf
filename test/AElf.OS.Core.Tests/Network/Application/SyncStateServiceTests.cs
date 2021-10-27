using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Node.Events;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol.Types;
using Moq;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.OS.Network.Application
{
    public class SyncStateServiceTests : SyncFlagTestBase
    {
        private readonly ISyncStateService _syncStateService;
        private readonly IPeerPool _peerPool;
        private readonly INodeSyncStateProvider _syncStateProvider;
        private readonly IBlockchainService _blockchainService;
        private readonly ILocalEventBus _eventBus;

        private readonly OSTestHelper _osTestHelper;

        public SyncStateServiceTests()
        {
            _syncStateService = GetRequiredService<ISyncStateService>();
            _peerPool = GetRequiredService<IPeerPool>();
            _syncStateProvider = GetRequiredService<INodeSyncStateProvider>();
            _osTestHelper = GetService<OSTestHelper>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _eventBus = GetRequiredService<ILocalEventBus>();
        }

        [Fact]
        public async Task StartSync_CurrentSyncStateIsNotUnInitialized_Test()
        {
            _syncStateProvider.SetSyncTarget(10);
            
            _peerPool.TryAddPeer(CreatePeer(15));
            _peerPool.TryAddPeer(CreatePeer(16));
            
            await _syncStateService.StartSyncAsync();
            _syncStateService.GetCurrentSyncTarget().ShouldBe(10);
            
        }
        
        [Fact]
        public async Task StartSync_NoEnoughPeer_Test()
        {
            _peerPool.TryAddPeer(CreatePeer(0));
            _peerPool.TryAddPeer(CreatePeer(16));
            
            InitialSyncFinishedEvent eventData = null;
            _eventBus.Subscribe<InitialSyncFinishedEvent>(args =>
            {
                eventData = args;
                return Task.CompletedTask;
            });
            
            await _syncStateService.StartSyncAsync();
            _syncStateService.GetCurrentSyncTarget().ShouldBe(-1);
            
            eventData.ShouldNotBeNull();
        }

        [Fact]
        public void Initial_State_Is_Syncing()
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
        public async Task UpdateSyncState_Test()
        {
            _peerPool.TryAddPeer(CreatePeer(15));
            _peerPool.TryAddPeer(CreatePeer(16));
            
            InitialSyncFinishedEvent eventData = null;
            _eventBus.Subscribe<InitialSyncFinishedEvent>(args =>
            {
                eventData = args;
                return Task.CompletedTask;
            });
            
            await _syncStateService.StartSyncAsync();
            _syncStateService.SyncState.ShouldBe(SyncState.Syncing);
            _syncStateService.GetCurrentSyncTarget().ShouldBe(15);
            
            await _syncStateService.UpdateSyncStateAsync();
            _syncStateService.SyncState.ShouldBe(SyncState.Syncing);
            _syncStateService.GetCurrentSyncTarget().ShouldBe(15);

            for (var i = 0; i < 4; i++)
            {
                await _osTestHelper.MinedOneBlock();
            }
            var chain = await _blockchainService.GetChainAsync();
            await _blockchainService.SetIrreversibleBlockAsync(chain, _osTestHelper.BestBranchBlockList.Last().Height,
                _osTestHelper.BestBranchBlockList.Last().GetHash());

            await _syncStateService.UpdateSyncStateAsync();
            _syncStateService.SyncState.ShouldBe(SyncState.Finished);
            _syncStateService.GetCurrentSyncTarget().ShouldBe(-1);
            
            eventData.ShouldNotBeNull();
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
                .Returns(new PeerConnectionInfo {Pubkey = CryptoHelper.GenerateKeyPair().PublicKey.ToHex()});
            
            peerMock.Setup(p => p.LastKnownLibHeight).Returns(libHeight);
            
            return peerMock.Object;
        }
    }
}