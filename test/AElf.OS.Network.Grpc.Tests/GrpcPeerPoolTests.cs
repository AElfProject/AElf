//using System;
//using System.Threading.Tasks;
//using AElf.Cryptography;
//using AElf.Kernel;
//using AElf.OS.Network.Grpc;
//using Google.Protobuf.WellKnownTypes;
//using Grpc.Core;
//using Shouldly;
//using Xunit;
//
//namespace AElf.OS.Network
//{
    // TODO
//    public class GrpcPeerPoolTests : GrpcNetworkTestBase
//    {
//        private readonly GrpcPeerPool _peerPool;
//
//        public GrpcPeerPoolTests()
//        {
//            _peerPool = GetRequiredService<GrpcPeerPool>();
//        }
//        
//        // todo move to handshake provider tests
////        [Fact]
////        public async Task GetHandshakeAsync()
////        {
////            var handshake = await _pool.GetHandshakeAsync();
////            handshake.ShouldNotBeNull();
////            handshake.HandshakeData.Version.ShouldBe(KernelConstants.ProtocolVersion);
////        }
//            
//        [Fact]
//        public void AddedPeer_IsFindable_ByAddressAndPubkey()
//        {
//            var peer = CreatePeer();
//            _peerPool.TryAddPeer(peer);
//            
//            _peerPool.PeerCount.ShouldBe(1);
//            _peerPool.FindPeerByAddress(peer.IpAddress).ShouldNotBeNull();
//            _peerPool.FindPeerByPublicKey(peer.Info.Pubkey).ShouldNotBeNull();
//        }
//
//        [Fact]
//        public async Task RemovePeerByPublicKey_ShouldNotBeFindable()
//        {
//            var peer = CreatePeer();
//            
//            _peerPool.TryAddPeer(peer);
//            await _peerPool.RemovePeerAsync(peer.Info.Pubkey, false);
//            
//            _peerPool.PeerCount.ShouldBe(0);
//            _peerPool.FindPeerByAddress(peer.IpAddress).ShouldBeNull();
//            _peerPool.FindPeerByPublicKey(peer.Info.Pubkey).ShouldBeNull();
//        }
//        
//        [Fact]
//        public async Task RemovePeerByAddress_ShouldNotBeFindable()
//        {
//            var peer = CreatePeer();
//            
//            _peerPool.TryAddPeer(peer);
//            await _peerPool.RemovePeerByAddressAsync(peer.IpAddress);
//            
//            _peerPool.PeerCount.ShouldBe(0);
//            _peerPool.FindPeerByAddress(peer.IpAddress).ShouldBeNull();
//            _peerPool.FindPeerByPublicKey(peer.Info.Pubkey).ShouldBeNull();
//        }
//
//        [Fact]
//        public async Task CannotAddPeerTwice()
//        {
//            var peer = CreatePeer();
//            _peerPool.TryAddPeer(peer);
//            _peerPool.TryAddPeer(peer).ShouldBeFalse();
//            
//            _peerPool.PeerCount.ShouldBe(1);
//            _peerPool.FindPeerByAddress(peer.IpAddress).ShouldNotBeNull();
//            _peerPool.FindPeerByPublicKey(peer.Info.Pubkey).ShouldNotBeNull();
//        }
//        
//        private static GrpcPeer CreatePeer(string ip = GrpcTestConstants.FakeIpEndpoint)
//        {
//            var keyPair = CryptoHelper.GenerateKeyPair();
//            var pubkey = keyPair.PublicKey.ToHex();
//            
//            var peerInfo = new PeerInfo
//            {
//                Pubkey = pubkey,
//                ProtocolVersion = KernelConstants.ProtocolVersion,
//                ConnectionTime = TimestampHelper.GetUtcNow().Seconds,
//                IsInbound = true
//            };
//            
//            return GrpcTestPeerFactory.CreatePeerWithInfo(ip, peerInfo);;
//        }
//    }
//}