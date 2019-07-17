using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Metrics;
using AElf.WebApp.Application.Net.Dto;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.WebApp.Application.Net.Tests
{
    public sealed class NetAppServiceTest: WebAppTestBase
    {
        private readonly IPeerPool _peerPool;
        public NetAppServiceTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _peerPool = GetRequiredService<IPeerPool>();
        }

        [Fact]
        public async Task AddPeerTest()
        { 
            var parameters = new Dictionary<string, string>
            {
                { "address","127.0.0.1:1680"}
            };
            
            var responseTrue = await PostResponseAsObjectAsync<bool>("/api/net/peer", parameters);
            responseTrue.ShouldBeFalse();
        }

        private IPeer BuildPeer(string ipAddress, string pubkey, long connectionTime, bool isInbound)
        {
            var connectionInfo = new PeerInfo
            {
                Pubkey = pubkey,
                ProtocolVersion = KernelConstants.ProtocolVersion,
                ConnectionTime = connectionTime,
                IsInbound = isInbound
            };
            
            var peerMock = new Mock<IPeer>();
            peerMock.SetupGet(p => p.Info).Returns(connectionInfo);
            peerMock.SetupGet(p => p.IpAddress).Returns(ipAddress);
            peerMock.Setup(p => p.GetRequestMetrics()).Returns(new Dictionary<string, List<RequestMetric>>());

            return peerMock.Object;
        }
        
        [Fact]
        public async Task GetPeersTest()
        {
            var connectionTime = TimestampHelper.GetUtcNow().Seconds;
            var ipAddressOne = "192.168.1.1:1680";
            var onePubkey = "048f5ced21f8d687cb9ade1c22dc0e183b05f87124c82073f5d82a09b139cc466efbfb6f28494d0a9d7366fcb769fe5436cfb7b5d322a2b0f69c4bcb1c33ac24ad";

            var peerOne = BuildPeer(ipAddressOne, onePubkey, connectionTime, true);
            _peerPool.TryAddPeer(peerOne);
            
            var ipAddressTwo = "192.168.1.2:1680";
            var twoPubkey = "040a7bf44d2c79fe5e270943773783a24eed5cda3e71fa49470cdba394a23832d5c831e233cddebea2720c194dffadd656d4dedf84643818ca77edeee17ad4307a";
            
            var peerTwo = BuildPeer(ipAddressTwo, twoPubkey, connectionTime, false);
            _peerPool.TryAddPeer(peerTwo);
            
            var peers = await GetResponseAsObjectAsync<List<PeerDto>>("api/net/peers");
            
            peers.Count.ShouldBe(2);
            
            peers.ShouldContain(peer => peer.IpAddress.IsIn(ipAddressOne, ipAddressTwo));
            peers.ShouldContain(peer => peer.ProtocolVersion == KernelConstants.ProtocolVersion);
            peers.ShouldContain(peer => peer.ConnectionTime == connectionTime);
            peers.ShouldContain(peer => peer.Inbound);
            peers.ShouldContain(peer => peer.Inbound == false);
        }
        
        [Fact]
        public async Task RemovePeerTest()
        {
            var connectionTime = TimestampHelper.GetUtcNow().Seconds;
            var ipAddressOne = "192.168.1.1:1680";
            var onePubkey = "048f5ced21f8d687cb9ade1c22dc0e183b05f87124c82073f5d82a09b139cc466efbfb6f28494d0a9d7366fcb769fe5436cfb7b5d322a2b0f69c4bcb1c33ac24ad";

            var peerOne = BuildPeer(ipAddressOne, onePubkey, connectionTime, true);
            _peerPool.TryAddPeer(peerOne);
            
            var ipAddressTwo = "192.168.1.2:1680";
            var twoPubkey = "040a7bf44d2c79fe5e270943773783a24eed5cda3e71fa49470cdba394a23832d5c831e233cddebea2720c194dffadd656d4dedf84643818ca77edeee17ad4307a";
            
            var peerTwo = BuildPeer(ipAddressTwo, twoPubkey, connectionTime, false);
            _peerPool.TryAddPeer(peerTwo);
            
            var response = await DeleteResponseAsObjectAsync<bool>($"/api/net/peer?address={ipAddressOne}");
            response.ShouldBeTrue();
            
            var peers = await GetResponseAsObjectAsync<List<PeerDto>>("/api/net/peers");
            peers.Count.ShouldBe(1);
            peers.ShouldContain(peer => peer.IpAddress.IsIn(ipAddressTwo));
        }

        [Fact]
        public async Task GetNetWorkInfoTest()
        {
            var connectionTime = TimestampHelper.GetUtcNow().Seconds;
            var ipAddressOne = "192.168.1.1:1680";
            var onePubkey = "048f5ced21f8d687cb9ade1c22dc0e183b05f87124c82073f5d82a09b139cc466efbfb6f28494d0a9d7366fcb769fe5436cfb7b5d322a2b0f69c4bcb1c33ac24ad";
            
            var peerOne = BuildPeer(ipAddressOne, onePubkey, connectionTime, true);
            _peerPool.TryAddPeer(peerOne);
            
            var ipAddressTwo = "192.168.1.2:1680";
            var twoPubkey = "040a7bf44d2c79fe5e270943773783a24eed5cda3e71fa49470cdba394a23832d5c831e233cddebea2720c194dffadd656d4dedf84643818ca77edeee17ad4307a";
            
            var peerTwo = BuildPeer(ipAddressTwo, twoPubkey, connectionTime, false);
            _peerPool.TryAddPeer(peerTwo);
            
            var peers = await GetResponseAsObjectAsync<List<PeerDto>>("api/net/peers");
            
            var networkInfo = await GetResponseAsObjectAsync<GetNetworkInfoOutput>("/api/net/networkInfo");
            networkInfo.Version.ShouldBe(typeof(NetApplicationWebAppAElfModule).Assembly.GetName().Version.ToString());
            networkInfo.ProtocolVersion.ShouldBe(KernelConstants.ProtocolVersion);
            networkInfo.Connections.ShouldBe(peers.Count);
        }
    }
}