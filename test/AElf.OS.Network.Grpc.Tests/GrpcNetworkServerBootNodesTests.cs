using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol.Types;
using AElf.Types;
using Grpc.Core;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.OS.Network
{
    public class GrpcNetworkServerBootNodesTests : GrpcNetworkWithBootNodesTestBase
    {
        private readonly IAElfNetworkServer _networkServer;
        private readonly ILocalEventBus _eventBus;


        public GrpcNetworkServerBootNodesTests()
        {
            _networkServer = GetRequiredService<IAElfNetworkServer>();
            _eventBus = GetRequiredService<ILocalEventBus>();
        }

        [Fact]
        public async Task Calling_DisposedPeer_ThrowsUnrecoverableNEtException()
        {
            await _networkServer.StartAsync();
            
            Channel channel = new Channel("localhost", 2001, ChannelCredentials.Insecure);
            PeerService.PeerServiceClient peerClient = new PeerService.PeerServiceClient(channel);
            
            GrpcClient grpcClient = new GrpcClient(channel, peerClient);

            AElfPeerEndpointHelper.TryParse("127.0.0.1:2001", out var endpoint);
            
            GrpcPeer peer = new GrpcPeer(grpcClient, endpoint, new PeerConnectionInfo
            {
                SessionId = new byte[] { 1,2,3 }
            });

            await peer.DisconnectAsync(false);

            var exHealthCheck = await Assert.ThrowsAsync<NetworkException>(async () => await peer.CheckHealthAsync());
            exHealthCheck.ExceptionType.ShouldBe(NetworkExceptionType.Unrecoverable);
            
            var exGetBlocks = await Assert.ThrowsAsync<NetworkException>(
                async () => await peer.GetBlocksAsync(Hash.FromString("blockHash"), 10));
            exGetBlocks.ExceptionType.ShouldBe(NetworkExceptionType.Unrecoverable);
            
            var exGetBlock = await Assert.ThrowsAsync<NetworkException>(
                async () => await peer.GetBlockByHashAsync(Hash.FromString("blockHash")));
            exGetBlock.ExceptionType.ShouldBe(NetworkExceptionType.Unrecoverable);
            
            var exGetNodes = await Assert.ThrowsAsync<NetworkException>(
                async () => await peer.GetNodesAsync());
            exGetNodes.ExceptionType.ShouldBe(NetworkExceptionType.Unrecoverable);
            
            await _networkServer.StopAsync();
        }

        [Fact]
        public async Task StartServer_Test()
        {
            NetworkInitializedEvent received = null;
            _eventBus.Subscribe<NetworkInitializedEvent>(a =>
            {
                received = a;
                return Task.CompletedTask;
            });
            
            await _networkServer.StartAsync();
            
            received.ShouldNotBeNull();
            
            await _networkServer.StopAsync();
        }
    }
}