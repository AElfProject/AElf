using System;
using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.OS.Network.Grpc;

public class GrpcNetworkServerTests : GrpcNetworkTestBase
{
    private readonly ILocalEventBus _eventBus;
    private readonly IAElfNetworkServer _networkServer;
    private readonly IPeerPool _peerPool;
    private readonly IReconnectionService _reconnectionService;

    public GrpcNetworkServerTests()
    {
        _networkServer = GetRequiredService<IAElfNetworkServer>();
        _eventBus = GetRequiredService<ILocalEventBus>();
        _peerPool = GetRequiredService<IPeerPool>();
        _reconnectionService = GetRequiredService<IReconnectionService>();
    }

    private GrpcPeer AddPeerToPool(string ip = NetworkTestConstants.FakeIpEndpoint,
        string pubkey = NetworkTestConstants.FakePubkey)
    {
        var peer = GrpcTestPeerHelper.CreateBasicPeer(ip, pubkey);
        var added = _peerPool.TryAddPeer(peer);

        Assert.True(added);

        return peer;
    }

    [Fact]
    public async Task Start_Test()
    {
        NetworkInitializedEvent eventData = null;
        _eventBus.Subscribe<NetworkInitializedEvent>(ed =>
        {
            eventData = ed;
            return Task.CompletedTask;
        });

        await _networkServer.StartAsync();
        await _networkServer.StopAsync();

        eventData.ShouldNotBeNull();
    }

    [Fact]
    public async Task Disconnect_Test()
    {
        await _networkServer.StartAsync();
        var peer = AddPeerToPool();
        peer.IsShutdown.ShouldBeFalse();
        await _networkServer.DisconnectAsync(peer);
        peer.IsShutdown.ShouldBeTrue();

        await _networkServer.StopAsync();
    }

    [Fact]
    public async Task TrySchedulePeerReconnection_Test()
    {
        var peer = AddPeerToPool();
        var result = await _networkServer.TrySchedulePeerReconnectionAsync(peer);
        result.ShouldBeTrue();

        _reconnectionService.GetReconnectingPeer(peer.RemoteEndpoint.ToString()).ShouldNotBeNull();
    }

    [Fact]
    public async Task DialPeerAsync_HostAlreadyInPool_ShouldReturnFalse()
    {
        var peer = AddPeerToPool();
        var added = await _networkServer.ConnectAsync(peer.RemoteEndpoint);

        added.ShouldBeFalse();
    }

    [Fact]
    public async Task DialPeerAsync_GoodPeer_ShouldBeInPool()
    {
        AElfPeerEndpointHelper.TryParse(NetworkTestConstants.GoodPeerEndpoint, out var endpoint);

        // two different hosts with the same pubkey.
        var added = await _networkServer.ConnectAsync(endpoint);

        added.ShouldBeTrue();
        _peerPool.FindPeerByEndpoint(endpoint).ShouldNotBeNull();
    }


    [Fact]
    public void DialPeerAsync_HandshakeNetProblem_ShouldThrowException()
    {
        AElfPeerEndpointHelper.TryParse(NetworkTestConstants.HandshakeWithNetExceptionIp, out var endpoint);
        _networkServer.ConnectAsync(endpoint).ShouldThrow<Exception>();

        _peerPool.PeerCount.ShouldBe(0);
    }

    [Fact]
    public void DialPeerAsync_HandshakeDataProblem_ShouldThrowException()
    {
        AElfPeerEndpointHelper.TryParse(NetworkTestConstants.HandshakeWithDataExceptionIp, out var endpoint);
        _networkServer.ConnectAsync(endpoint).ShouldThrow<Exception>();

        _peerPool.PeerCount.ShouldBe(0);
    }

    [Fact]
    public void DialPeerAsync_HandshakeError_ShouldThrowException()
    {
        AElfPeerEndpointHelper.TryParse(NetworkTestConstants.BadHandshakeIp, out var endpoint);
        _networkServer.ConnectAsync(endpoint).ShouldThrow<NetworkException>();

        _peerPool.PeerCount.ShouldBe(0);
    }
}