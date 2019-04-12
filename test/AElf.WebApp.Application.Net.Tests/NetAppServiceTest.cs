using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
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
            var ipAddressOne = "192.168.1.1:1680";
            var channelOne = new Channel(ipAddressOne, ChannelCredentials.Insecure);
            var peerOne = new GrpcPeer(channelOne, new PeerService.PeerServiceClient(channelOne),
                "0454dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab399",
                ipAddressOne);
            _peerPool.AddPeer(peerOne);
            
            var ipAddressTwo = "192.168.1.2:1680";
            var channelTwo = new Channel(ipAddressTwo, ChannelCredentials.Insecure);
            var peerTwo = new GrpcPeer(channelTwo, new PeerService.PeerServiceClient(channelTwo),
                "0624dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab390",
                ipAddressTwo);
            _peerPool.AddPeer(peerTwo);
            var peers = await GetResponseAsObjectAsync<List<string>>("api/net/peers");
            peers.Count.ShouldBe(2);
            peers.ShouldContain((peer)=>peer.IsIn(ipAddressOne,ipAddressTwo));
        }
        
        [Fact]
        public async Task RemovePeerTest()
        {
            var ipAddressOne = "192.168.1.1:1680";
            var channelOne = new Channel(ipAddressOne, ChannelCredentials.Insecure);
            var peerOne = new GrpcPeer(channelOne, new PeerService.PeerServiceClient(channelOne),
                "0454dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab399",
                ipAddressOne);
            _peerPool.AddPeer(peerOne);
            
            var ipAddressTwo = "192.168.1.2:1680";
            var channelTwo = new Channel(ipAddressTwo, ChannelCredentials.Insecure);
            var peerTwo = new GrpcPeer(channelTwo, new PeerService.PeerServiceClient(channelTwo),
                "0624dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab390",
                ipAddressTwo);
            _peerPool.AddPeer(peerTwo);
            
            var response = await DeleteResponseAsObjectAsync<bool>($"/api/net/peer?address={ipAddressOne}");
            response.ShouldBeTrue();
            
            var peers = await GetResponseAsObjectAsync<List<string>>("/api/net/peers");
            peers.Count.ShouldBe(1);
            peers.ShouldContain((peer)=>peer.IsIn(ipAddressTwo));
        }
    }
}