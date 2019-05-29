using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using AElf.WebApp.Application.Net.Dto;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
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
            var responseTrue = await PostResponseAsObjectAsync<bool>("/api/net/peer",parameters);
            responseTrue.ShouldBeFalse();
        }
        
        [Fact]
        public async Task GetPeersTest()
        {
            var connectionTime = DateTimeHelper.Now.ToTimestamp().Seconds;
            var startHeight = 1;
            var ipAddressOne = "192.168.1.1:1680";
            var channelOne = new Channel(ipAddressOne, ChannelCredentials.Insecure);
            var peerOne = new GrpcPeer(channelOne, new PeerService.PeerServiceClient(channelOne),
                "0454dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab399",
                ipAddressOne, KernelConstants.ProtocolVersion, connectionTime, startHeight);
            _peerPool.AddPeer(peerOne);
            
            var ipAddressTwo = "192.168.1.2:1680";
            var channelTwo = new Channel(ipAddressTwo, ChannelCredentials.Insecure);
            var peerTwo = new GrpcPeer(channelTwo, new PeerService.PeerServiceClient(channelTwo),
                "0624dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab390",
                ipAddressTwo, KernelConstants.ProtocolVersion, connectionTime, startHeight, false);
            _peerPool.AddPeer(peerTwo);
            var peers = await GetResponseAsObjectAsync<List<PeerDto>>("api/net/peers");
            peers.Count.ShouldBe(2);
            peers.ShouldContain(peer => peer.IpAddress.IsIn(ipAddressOne, ipAddressTwo));
            peers.ShouldContain(peer => peer.ProtocolVersion == KernelConstants.ProtocolVersion);
            peers.ShouldContain(peer => peer.ConnectionTime == connectionTime);
            peers.ShouldContain(peer => peer.Inbound);
            peers.ShouldContain(peer => peer.Inbound == false);
            peers.ShouldContain(peer => peer.StartHeight == startHeight);
        }
        
        [Fact]
        public async Task RemovePeerTest()
        {
            var connectionTime = DateTimeHelper.Now.ToTimestamp().Seconds;
            var ipAddressOne = "192.168.1.1:1680";
            var channelOne = new Channel(ipAddressOne, ChannelCredentials.Insecure);
            var peerOne = new GrpcPeer(channelOne, new PeerService.PeerServiceClient(channelOne),
                "0454dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab399",
                ipAddressOne, KernelConstants.ProtocolVersion, connectionTime, 1);
            _peerPool.AddPeer(peerOne);
            
            var ipAddressTwo = "192.168.1.2:1680";
            var channelTwo = new Channel(ipAddressTwo, ChannelCredentials.Insecure);
            var peerTwo = new GrpcPeer(channelTwo, new PeerService.PeerServiceClient(channelTwo),
                "0624dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab390",
                ipAddressTwo, KernelConstants.ProtocolVersion, connectionTime, 1);
            _peerPool.AddPeer(peerTwo);
            
            var response = await DeleteResponseAsObjectAsync<bool>($"/api/net/peer?address={ipAddressOne}");
            response.ShouldBeTrue();
            
            var peers = await GetResponseAsObjectAsync<List<PeerDto>>("/api/net/peers");
            peers.Count.ShouldBe(1);
            peers.ShouldContain(peer => peer.IpAddress.IsIn(ipAddressTwo));
        }

        [Fact]
        public async Task GetNetWorkInfoTest()
        {
            var connectionTime = DateTimeHelper.Now.ToTimestamp().Seconds;
            var ipAddressOne = "192.168.1.1:1680";
            var channelOne = new Channel(ipAddressOne, ChannelCredentials.Insecure);
            var peerOne = new GrpcPeer(channelOne, new PeerService.PeerServiceClient(channelOne),
                "0454dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab399",
                ipAddressOne, KernelConstants.ProtocolVersion, connectionTime, 1);
            _peerPool.AddPeer(peerOne);
            
            var ipAddressTwo = "192.168.1.2:1680";
            var channelTwo = new Channel(ipAddressTwo, ChannelCredentials.Insecure);
            var peerTwo = new GrpcPeer(channelTwo, new PeerService.PeerServiceClient(channelTwo),
                "0624dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab390",
                ipAddressTwo, KernelConstants.ProtocolVersion, connectionTime, 1);
            _peerPool.AddPeer(peerTwo);
            
            var peers = await GetResponseAsObjectAsync<List<PeerDto>>("api/net/peers");
            
            var networkInfo = await GetResponseAsObjectAsync<GetNetworkInfoOutput>("/api/net/networkInfo");
            networkInfo.Version.ShouldBe(typeof(NetApplicationWebAppAElfModule).Assembly.GetName().Version.ToString());
            networkInfo.ProtocolVersion.ShouldBe(KernelConstants.ProtocolVersion);
            networkInfo.Connections.ShouldBe(peers.Count);
        }
    }
}