using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.OS.Network;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using AElf.TestBase;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Shouldly;
using Xunit;

namespace AElf.OS.Consensus.DPos
{
    // ReSharper disable once InconsistentNaming
    public sealed class AEDPoSLastLastIrreversibleBlockDiscoveryServiceTests_FullNode : AElfIntegratedTest<OSConsensusDPosTestModule_FullNode>
    {
        private readonly IAEDPoSLastLastIrreversibleBlockDiscoveryService
            _aedpoSLastLastIrreversibleBlockDiscoveryService;
        private readonly IPeerPool _peerPool;
        private readonly OSTestHelper _osTestHelper;

        private readonly long _connectionTime = TimestampHelper.GetUtcNow().Seconds;

        public AEDPoSLastLastIrreversibleBlockDiscoveryServiceTests_FullNode()
        {
            _aedpoSLastLastIrreversibleBlockDiscoveryService =
                GetRequiredService<IAEDPoSLastLastIrreversibleBlockDiscoveryService>();
            _peerPool = GetRequiredService<IPeerPool>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
        }

        [Fact]
        public async Task Find_LIB_Return_Null()
        {
            var blockIndex =
                await _aedpoSLastLastIrreversibleBlockDiscoveryService.FindLastLastIrreversibleBlockAsync();
            blockIndex.ShouldBeNull();
            
            AddPeer(OSConsensusDPosTestConstants.FullNodePubKey,5);
            blockIndex =
                await _aedpoSLastLastIrreversibleBlockDiscoveryService.FindLastLastIrreversibleBlockAsync();
            blockIndex.ShouldBeNull();
            
            AddPeer(OSConsensusDPosTestConstants.Bp1PublicKey,5);
            blockIndex =
                await _aedpoSLastLastIrreversibleBlockDiscoveryService.FindLastLastIrreversibleBlockAsync();
            blockIndex.ShouldBeNull();
        }

        [Fact(Skip = "Skip temporary")]
        public async Task Find_LIB_With_Three_BP_Peers_Return_Block_Index()
        {
            var blocks = _osTestHelper.BestBranchBlockList;
            AddPeer(OSConsensusDPosTestConstants.Bp1PublicKey, 5);
            AddPeer(OSConsensusDPosTestConstants.Bp2PublicKey, 6);
            AddPeer(OSConsensusDPosTestConstants.Bp3PublicKey, 7);
            
            var blockIndex = await _aedpoSLastLastIrreversibleBlockDiscoveryService.FindLastLastIrreversibleBlockAsync();
            blockIndex.Height.ShouldBe(blocks[4].Height);
            blockIndex.Hash.ShouldBe(blocks[4].GetHash());
        }
        
        [Fact]
        public async Task Find_LIB_With_Two_BP_Peers_Return_Null()
        {
            AddPeer(OSConsensusDPosTestConstants.Bp1PublicKey, 5);
            AddPeer(OSConsensusDPosTestConstants.Bp2PublicKey, 6);
            
            var blockIndex = await _aedpoSLastLastIrreversibleBlockDiscoveryService.FindLastLastIrreversibleBlockAsync();
            blockIndex.ShouldBeNull();
        }

        private void AddPeer(string publicKey,int blockHeight)
        {
            var channel = new Channel(OSConsensusDPosTestConstants.FakeListeningPort, ChannelCredentials.Insecure);
            
            var connectionInfo = new GrpcPeerInfo
            {
                PublicKey = publicKey,
                PeerIpAddress = OSConsensusDPosTestConstants.FakeListeningPort,
                ProtocolVersion = KernelConstants.ProtocolVersion,
                ConnectionTime = _connectionTime,
                StartHeight = 1,
                IsInbound = true
            };
            
            var peer = new GrpcPeer(channel, new PeerService.PeerServiceClient(channel), connectionInfo);
            var blocks = _osTestHelper.BestBranchBlockList.GetRange(0, blockHeight);
            foreach (var block in blocks)
            {
                peer.HandlerRemotePreLibAnnounce(new PeerPreLibAnnouncement
                    {BlockHash = block.GetHash(), BlockHeight = block.Height});
            }
            _peerPool.AddPeer(peer);
        }
    }
}