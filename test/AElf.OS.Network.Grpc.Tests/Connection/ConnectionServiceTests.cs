using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Testing;
using Moq;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.OS.Network.Grpc;

public class ConnectionServiceTests : GrpcNetworkTestBase
{
    private readonly IBlackListedPeerProvider _blackListedPeerProvider;
    private readonly IBlockchainService _blockchainService;
    private readonly IConnectionService _connectionService;
    private readonly ILocalEventBus _eventBus;
    private readonly IHandshakeProvider _handshakeProvider;
    private readonly IPeerPool _peerPool;
    private readonly IReconnectionService _reconnectionService;

    public ConnectionServiceTests()
    {
        _connectionService = GetRequiredService<IConnectionService>();
        _peerPool = GetRequiredService<IPeerPool>();
        _blackListedPeerProvider = GetRequiredService<IBlackListedPeerProvider>();
        _eventBus = GetRequiredService<ILocalEventBus>();
        _blockchainService = GetRequiredService<IBlockchainService>();
        _handshakeProvider = GetRequiredService<IHandshakeProvider>();
        _reconnectionService = GetRequiredService<IReconnectionService>();
    }

    [Fact]
    public async Task Connect_DialPeerFailed_Test()
    {
        AElfPeerEndpointHelper.TryParse(NetworkTestConstants.DialExceptionIpEndpoint, out var endpoint);
        var result = await _connectionService.ConnectAsync(endpoint);
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task Connect_PeerAlreadyInPoolAndIsValid_Test()
    {
        var peer = CreatePeerAndAddToPeerPool();
        var added = await _connectionService.ConnectAsync(peer.RemoteEndpoint);

        added.ShouldBeFalse();
    }

    [Fact]
    public async Task Connect_PeerAlreadyInPoolAndIsInvalid_Test()
    {
        var peer = CreatePeerAndAddToPeerPool(NetworkTestConstants.GoodPeerEndpoint, NetworkTestConstants.FakePubkey2);
        peer.IsConnected = false;
        peer.Info.ConnectionTime = TimestampHelper.GetUtcNow()
            .AddMilliseconds(-NetworkConstants.PeerConnectionTimeout - 1000);

        var added = await _connectionService.ConnectAsync(peer.RemoteEndpoint);
        added.ShouldBeTrue();

        var currentPeer = _peerPool.FindPeerByPublicKey(NetworkTestConstants.FakePubkey);
        currentPeer.ShouldNotBeNull();
    }

    [Fact]
    public async Task Connect_HostInBlackList_Test()
    {
        AElfPeerEndpointHelper.TryParse(NetworkTestConstants.FakeIpEndpoint, out var endpoint);
        _blackListedPeerProvider.AddHostToBlackList(endpoint.Host, 10);

        var added = await _connectionService.ConnectAsync(endpoint);
        added.ShouldBeFalse();
    }

    [Fact]
    public async Task Connect_OverIpLimit_Test()
    {
        AElfPeerEndpointHelper.TryParse("192.168.100.100", out var endpoint);
        _peerPool.AddHandshakingPeer(endpoint.Host, "pubkey");

        var added = await _connectionService.ConnectAsync(endpoint);
        added.ShouldBeFalse();
    }

    [Fact]
    public async Task Connect_InboundPeerIsLater_Test()
    {
        var peer = CreatePeerAndAddToPeerPool();

        peer.UpdateLastReceivedHandshake(new Handshake
        {
            HandshakeData = new HandshakeData
            {
                LastIrreversibleBlockHeight = 1,
                BestChainHash = HashHelper.ComputeFrom("BestChainHash"),
                BestChainHeight = 10,
                Time = TimestampHelper.GetUtcNow().AddMinutes(1)
            }
        });

        AElfPeerEndpointHelper.TryParse(NetworkTestConstants.GoodPeerEndpoint, out var endpoint);

        var added = await _connectionService.ConnectAsync(endpoint);
        added.ShouldBeTrue();

        var currentPeer = _peerPool.FindPeerByEndpoint(endpoint);
        currentPeer.ShouldNotBeNull();

        AElfPeerEndpointHelper.TryParse(NetworkTestConstants.FakeIpEndpoint, out endpoint);
        currentPeer = _peerPool.FindPeerByEndpoint(endpoint);
        currentPeer.ShouldBeNull();
    }

    [Fact]
    public async Task Connect_InboundPeerIsEarlier_Test()
    {
        var mockClient = new Mock<PeerService.PeerServiceClient>();
        var confirmHandshakeCall = TestCalls.AsyncUnaryCall(Task.FromResult(new VoidReply()),
            Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(),
            () => { });
        mockClient.Setup(m => m.ConfirmHandshakeAsync(It.IsAny<ConfirmHandshakeRequest>(),
            It.IsAny<Metadata>(), It.IsAny<DateTime?>(),
            CancellationToken.None)).Returns(confirmHandshakeCall);

        var peer = GrpcTestPeerHelper.CreatePeerWithClient(NetworkTestConstants.FakeIpEndpoint,
            NetworkTestConstants.FakePubkey, mockClient.Object);

        peer.UpdateLastReceivedHandshake(new Handshake
        {
            HandshakeData = new HandshakeData
            {
                LastIrreversibleBlockHeight = 1,
                BestChainHash = HashHelper.ComputeFrom("BestChainHash"),
                BestChainHeight = 10,
                Time = TimestampHelper.GetUtcNow().AddMinutes(-1)
            }
        });
        _peerPool.TryAddPeer(peer);

        AElfPeerEndpointHelper.TryParse(NetworkTestConstants.GoodPeerEndpoint, out var endpoint);

        var added = await _connectionService.ConnectAsync(endpoint);
        added.ShouldBeTrue();

        var currentPeer = _peerPool.FindPeerByEndpoint(endpoint);
        currentPeer.ShouldBeNull();

        AElfPeerEndpointHelper.TryParse(NetworkTestConstants.FakeIpEndpoint, out endpoint);
        currentPeer = _peerPool.FindPeerByEndpoint(endpoint);
        currentPeer.ShouldNotBeNull();
    }

    [Fact]
    public async Task Connect_ConfirmFailed_Test()
    {
        var mockClient = new Mock<PeerService.PeerServiceClient>();
        mockClient.Setup(m => m.ConfirmHandshakeAsync(It.IsAny<ConfirmHandshakeRequest>(),
            It.IsAny<Metadata>(), It.IsAny<DateTime?>(),
            CancellationToken.None)).Throws<Exception>();

        var peer = GrpcTestPeerHelper.CreatePeerWithClient(NetworkTestConstants.FakeIpEndpoint,
            NetworkTestConstants.FakePubkey, mockClient.Object);

        peer.UpdateLastReceivedHandshake(new Handshake
        {
            HandshakeData = new HandshakeData
            {
                LastIrreversibleBlockHeight = 1,
                BestChainHash = HashHelper.ComputeFrom("BestChainHash"),
                BestChainHeight = 10,
                Time = TimestampHelper.GetUtcNow().AddMinutes(-1)
            }
        });
        _peerPool.TryAddPeer(peer);

        AElfPeerEndpointHelper.TryParse(NetworkTestConstants.GoodPeerEndpoint, out var endpoint);

        _connectionService.ConnectAsync(endpoint).ShouldThrow<Exception>();

        var currentPeer = _peerPool.FindPeerByEndpoint(endpoint);
        currentPeer.ShouldBeNull();

        AElfPeerEndpointHelper.TryParse(NetworkTestConstants.FakeIpEndpoint, out endpoint);
        currentPeer = _peerPool.FindPeerByEndpoint(endpoint);
        currentPeer.ShouldBeNull();
    }

    [Fact]
    public async Task DialPeerAsync_GoodPeer_Test()
    {
        PeerConnectedEventData eventData = null;
        _eventBus.Subscribe<PeerConnectedEventData>(e =>
        {
            eventData = e;
            return Task.CompletedTask;
        });

        AElfPeerEndpointHelper.TryParse(NetworkTestConstants.GoodPeerEndpoint, out var endpoint);

        var added = await _connectionService.ConnectAsync(endpoint);
        added.ShouldBeTrue();

        var peer = (GrpcPeer)_peerPool.FindPeerByEndpoint(endpoint);
        peer.ShouldNotBeNull();
        peer.IsConnected.ShouldBeTrue();
        peer.SyncState.ShouldBe(SyncState.Syncing);
        var nodeVersion = typeof(CoreOSAElfModule).Assembly.GetName().Version?.ToString();
        peer.Info.NodeVersion.ShouldBe(nodeVersion);

        eventData.ShouldNotBeNull();
    }

    [Fact]
    public async Task DoHandshake_InvalidHandshake_Test()
    {
        AElfPeerEndpointHelper.TryParse(NetworkTestConstants.GoodPeerEndpoint, out var endpoint);

        var handshake = CreateHandshake();
        handshake.HandshakeData.ChainId = 100;
        var result = await _connectionService.DoHandshakeAsync(endpoint, handshake);
        result.Error.ShouldBe(HandshakeError.ChainMismatch);

        handshake = CreateHandshake();
        handshake.HandshakeData.Version = 100;
        result = await _connectionService.DoHandshakeAsync(endpoint, handshake);
        result.Error.ShouldBe(HandshakeError.ProtocolMismatch);

        handshake = CreateHandshake();
        handshake.HandshakeData.Time = handshake.HandshakeData.Time -
                                       TimestampHelper.DurationFromMilliseconds(
                                           NetworkConstants.HandshakeTimeout + 1000);
        result = await _connectionService.DoHandshakeAsync(endpoint, handshake);
        result.Error.ShouldBe(HandshakeError.SignatureTimeout);

        handshake = CreateHandshake();
        handshake.HandshakeData.Pubkey = ByteString.CopyFrom(CryptoHelper.GenerateKeyPair().PublicKey);
        result = await _connectionService.DoHandshakeAsync(endpoint, handshake);
        result.Error.ShouldBe(HandshakeError.WrongSignature);

        handshake = await _handshakeProvider.GetHandshakeAsync();
        result = await _connectionService.DoHandshakeAsync(endpoint, handshake);
        result.Error.ShouldBe(HandshakeError.ConnectionRefused);
    }

    [Fact]
    public async Task DoHandshake_AlreadyInHandshaking_Test()
    {
        AElfPeerEndpointHelper.TryParse(NetworkTestConstants.GoodPeerEndpoint, out var endpoint);
        var handshake = CreateHandshake();
        _peerPool.AddHandshakingPeer(endpoint.Host, handshake.HandshakeData.Pubkey.ToHex());

        var result = await _connectionService.DoHandshakeAsync(endpoint, handshake);
        result.Error.ShouldBe(HandshakeError.ConnectionRefused);

        _peerPool.GetHandshakingPeers().ShouldNotContainKey(endpoint.Host);
    }

    [Fact]
    public async Task DoHandshake_DialBackFailed_Test()
    {
        AElfPeerEndpointHelper.TryParse(NetworkTestConstants.FakeIpEndpoint, out var endpoint);
        var handshake = CreateHandshake(port: endpoint.Port);

        var result = await _connectionService.DoHandshakeAsync(endpoint, handshake);
        result.Error.ShouldBe(HandshakeError.InvalidConnection);

        _peerPool.GetHandshakingPeers().ShouldNotContainKey(endpoint.Host);
    }

    [Fact]
    public async Task DoHandshake_AlreadyInPeerPool_Test()
    {
        var keyPair = CryptoHelper.GenerateKeyPair();
        var existPeer = CreatePeerAndAddToPeerPool(pubkey: keyPair.PublicKey.ToHex());

        AElfPeerEndpointHelper.TryParse(NetworkTestConstants.GoodPeerEndpoint, out var endpoint);
        var handshake = CreateHandshake(keyPair, endpoint.Port);

        var result = await _connectionService.DoHandshakeAsync(endpoint, handshake);
        result.Error.ShouldBe(HandshakeError.RepeatedConnection);
    }

    [Fact]
    public async Task DoHandshake_Success_Test()
    {
        AElfPeerEndpointHelper.TryParse(NetworkTestConstants.GoodPeerEndpoint, out var endpoint);
        var handshake = CreateHandshake(port: endpoint.Port);

        var result = await _connectionService.DoHandshakeAsync(endpoint, handshake);
        result.Error.ShouldBe(HandshakeError.HandshakeOk);

        var peer = (GrpcPeer)_peerPool.FindPeerByEndpoint(endpoint);
        peer.InboundSessionId.ShouldBe(result.Handshake.SessionId);
        peer.LastSentHandshakeTime.ShouldBe(result.Handshake.HandshakeData.Time);

        _peerPool.GetHandshakingPeers().ShouldNotContainKey(endpoint.Host);
    }

    [Fact]
    public async Task GetPeerByPubkey_Test()
    {
        var peer = CreatePeerAndAddToPeerPool(pubkey: NetworkTestConstants.FakePubkey);
        _connectionService.GetPeerByPubkey(NetworkTestConstants.FakePubkey).ShouldBe(peer);

        await _connectionService.RemovePeerAsync(NetworkTestConstants.FakePubkey);
        _connectionService.GetPeerByPubkey(NetworkTestConstants.FakePubkey).ShouldBeNull();
    }

    [Fact]
    public async Task Disconnect_Test()
    {
        var peer1 = CreatePeerAndAddToPeerPool(pubkey: NetworkTestConstants.FakePubkey);
        var peer2 = CreatePeerAndAddToPeerPool(pubkey: NetworkTestConstants.FakePubkey2);

        _reconnectionService.SchedulePeerForReconnection(NetworkTestConstants.FakeIpEndpoint);

        await _connectionService.DisconnectAsync(peer1);
        peer1.IsConnected.ShouldBeFalse();
        peer1.IsShutdown.ShouldBeTrue();

        _reconnectionService.GetReconnectingPeer(NetworkTestConstants.FakeIpEndpoint).ShouldBeNull();

        _connectionService.GetPeerByPubkey(NetworkTestConstants.FakePubkey).ShouldBeNull();
        _connectionService.GetPeerByPubkey(NetworkTestConstants.FakePubkey2).ShouldNotBeNull();
    }

    [Fact]
    public async Task DisconnectPeers_Test()
    {
        var peer1 = CreatePeerAndAddToPeerPool(pubkey: NetworkTestConstants.FakePubkey);
        var peer2 = CreatePeerAndAddToPeerPool(pubkey: NetworkTestConstants.FakePubkey2);

        await _connectionService.DisconnectPeersAsync(false);
        peer1.IsConnected.ShouldBeFalse();
        peer1.IsShutdown.ShouldBeTrue();
        peer2.IsConnected.ShouldBeFalse();
        peer2.IsShutdown.ShouldBeTrue();
    }

    [Fact]
    public async Task SchedulePeerReconnection_Test()
    {
        AElfPeerEndpointHelper.TryParse(NetworkTestConstants.FakeIpEndpoint, out var endpoint);
        var result = await _connectionService.SchedulePeerReconnection(endpoint);
        result.ShouldBeTrue();

        _reconnectionService.GetReconnectingPeer(NetworkTestConstants.FakeIpEndpoint).ShouldNotBeNull();
    }

    [Fact]
    public async Task TrySchedulePeerReconnection_IsOutbound_Test()
    {
        var peer = CreatePeerAndAddToPeerPool(pubkey: NetworkTestConstants.FakePubkey);
        peer.Info.IsInbound = false;

        var result = await _connectionService.TrySchedulePeerReconnectionAsync(peer);
        result.ShouldBeTrue();

        peer.IsConnected.ShouldBeFalse();
        peer.IsShutdown.ShouldBeTrue();

        _reconnectionService.GetReconnectingPeer(NetworkTestConstants.FakeIpEndpoint).ShouldNotBeNull();
    }

    [Fact]
    public async Task TrySchedulePeerReconnection_IsInboundAndNotInBootNode_Test()
    {
        var peer = CreatePeerAndAddToPeerPool(pubkey: NetworkTestConstants.FakePubkey);
        peer.Info.IsInbound = true;

        var result = await _connectionService.TrySchedulePeerReconnectionAsync(peer);
        result.ShouldBeFalse();

        peer.IsConnected.ShouldBeFalse();
        peer.IsShutdown.ShouldBeTrue();

        _reconnectionService.GetReconnectingPeer(NetworkTestConstants.FakeIpEndpoint).ShouldBeNull();
    }

    [Fact]
    public void ConfirmHandshake_Test()
    {
        PeerConnectedEventData eventData = null;
        _eventBus.Subscribe<PeerConnectedEventData>(e =>
        {
            eventData = e;
            return Task.CompletedTask;
        });

        _connectionService.ConfirmHandshake(NetworkTestConstants.FakePubkey);
        eventData.ShouldBeNull();

        var peer = CreatePeerAndAddToPeerPool();
        _connectionService.ConfirmHandshake(NetworkTestConstants.FakePubkey);
        peer.IsConnected.ShouldBeTrue();
        peer.SyncState.ShouldBe(SyncState.Syncing);

        eventData.ShouldNotBeNull();
    }

    private Handshake CreateHandshake(ECKeyPair initiatorPeer = null, int port = 0)
    {
        if (initiatorPeer == null)
            initiatorPeer = CryptoHelper.GenerateKeyPair();

        var data = new HandshakeData
        {
            ChainId = _blockchainService.GetChainId(),
            Version = KernelConstants.ProtocolVersion,
            Pubkey = ByteString.CopyFrom(initiatorPeer.PublicKey),
            Time = TimestampHelper.GetUtcNow(),
            ListeningPort = port
        };

        var signature =
            CryptoHelper.SignWithPrivateKey(initiatorPeer.PrivateKey, HashHelper.ComputeFrom(data).ToByteArray());

        return new Handshake { HandshakeData = data, Signature = ByteString.CopyFrom(signature) };
    }

    private GrpcPeer CreatePeerAndAddToPeerPool(string ip = NetworkTestConstants.FakeIpEndpoint,
        string pubkey = NetworkTestConstants.FakePubkey)
    {
        var peer = GrpcTestPeerHelper.CreateBasicPeer(ip, pubkey);
        var added = _peerPool.TryAddPeer(peer);

        Assert.True(added);

        return peer;
    }
}