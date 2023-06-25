using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Modularity;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol;
using AElf.Types;
using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS.Network.Grpc;

[DependsOn(typeof(GrpcNetworkBaseTestModule))]
public class GrpcNetworkTestModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<NetworkOptions>(o =>
        {
            o.ListeningPort = 2001;
            o.MaxPeers = 2;
        });

        context.Services.AddTransient(o =>
        {
            var mockBlockchainService = new Mock<IBlockchainService>();
            var keyPair = CryptoHelper.GenerateKeyPair();

            mockBlockchainService.Setup(b => b.GetChainAsync()).ReturnsAsync(new Chain
            {
                Id = NetworkTestConstants.DefaultChainId
            });

            mockBlockchainService.Setup(b => b.GetChainId()).Returns(NetworkTestConstants.DefaultChainId);

            mockBlockchainService.Setup(b => b.GetBlockHeaderByHashAsync(It.IsAny<Hash>())).ReturnsAsync(
                NetworkTestHelper.CreateFakeBlockHeader(NetworkTestConstants.DefaultChainId, 1, keyPair));

            return mockBlockchainService.Object;
        });

        context.Services.AddTransient(sp =>
        {
            var mockDialer = new Mock<IPeerDialer>();

            mockDialer.Setup(d =>
                    d.DialPeerAsync(It.Is<DnsEndPoint>(endPoint =>
                        endPoint.ToString() == NetworkTestConstants.FakeIpEndpoint)))
                .Returns<DnsEndPoint>(s =>
                {
                    var peer = GrpcTestPeerHelper.CreateBasicPeer(NetworkTestConstants.FakeIpEndpoint,
                        NetworkTestConstants.FakePubkey);
                    return Task.FromResult(peer);
                });

            mockDialer.Setup(d =>
                    d.DialPeerAsync(It.Is<DnsEndPoint>(endPoint =>
                        endPoint.ToString() == NetworkTestConstants.FakeIpEndpoint2)))
                .Returns<DnsEndPoint>(s =>
                {
                    var peer = GrpcTestPeerHelper.CreateBasicPeer(NetworkTestConstants.FakeIpEndpoint2,
                        NetworkTestConstants.FakePubkey);
                    return Task.FromResult(peer);
                });

            mockDialer.Setup(d => d.DialPeerAsync(It.Is<DnsEndPoint>(endPoint =>
                    endPoint.ToString() == NetworkTestConstants
                        .DialExceptionIpEndpoint)))
                .Returns(Task.FromResult<GrpcPeer>(null));

            mockDialer.Setup(d => d.DialPeerAsync(It.Is<DnsEndPoint>(endPoint =>
                    endPoint.ToString() == NetworkTestConstants.HandshakeWithNetExceptionIp)))
                .Returns<DnsEndPoint>(s =>
                {
                    var mockClient = new Mock<PeerService.PeerServiceClient>();
                    mockClient.Setup(m => m.DoHandshakeAsync(It.IsAny<HandshakeRequest>(), It.IsAny<Metadata>(),
                            It.IsAny<DateTime?>(), CancellationToken.None))
                        .Throws(new AggregateException());

                    var peer = GrpcTestPeerHelper.CreatePeerWithClient(NetworkTestConstants.FakeIpEndpoint2,
                        NetworkTestConstants.FakePubkey,
                        mockClient.Object);

                    return Task.FromResult(peer as GrpcPeer);
                });

            // Incorrect handshake
            mockDialer.Setup(d =>
                    d.DialPeerAsync(It.Is<DnsEndPoint>(endPoint =>
                        endPoint.ToString() == NetworkTestConstants.BadHandshakeIp)))
                .Returns<DnsEndPoint>(s =>
                {
                    var handshakeReply = new HandshakeReply();

                    var handshakeCall = TestCalls.AsyncUnaryCall(Task.FromResult(handshakeReply),
                        Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(),
                        () => { });

                    var mockClient = new Mock<PeerService.PeerServiceClient>();
                    mockClient.Setup(m => m.DoHandshakeAsync(It.IsAny<HandshakeRequest>(), It.IsAny<Metadata>(),
                        It.IsAny<DateTime?>(),
                        CancellationToken.None)).Returns(handshakeCall);
                    mockClient.Setup(m => m.ConfirmHandshakeAsync(It.IsAny<ConfirmHandshakeRequest>(),
                        It.IsAny<Metadata>(), It.IsAny<DateTime?>(),
                        CancellationToken.None)).Throws(new AggregateException());

                    var peer = GrpcTestPeerHelper.CreatePeerWithClient(NetworkTestConstants.GoodPeerEndpoint,
                        NetworkTestConstants.FakePubkey, mockClient.Object);

                    return Task.FromResult(peer as GrpcPeer);
                });

            // Incorrect handshake signature
            mockDialer.Setup(d => d.DialPeerAsync(It.Is<DnsEndPoint>(endPoint =>
                    endPoint.ToString() == NetworkTestConstants
                        .HandshakeWithDataExceptionIp)))
                .Returns<string>(async s =>
                {
                    var handshakeProvider = context.Services.GetServiceLazy<IHandshakeProvider>().Value;
                    var handshake = await handshakeProvider.GetHandshakeAsync();
                    handshake.HandshakeData.Time = null;
                    var handshakeReply = new HandshakeReply { Handshake = handshake };

                    var handshakeCall = TestCalls.AsyncUnaryCall(Task.FromResult(handshakeReply),
                        Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(),
                        () => { });

                    var mockClient = new Mock<PeerService.PeerServiceClient>();
                    mockClient.Setup(m => m.DoHandshakeAsync(It.IsAny<HandshakeRequest>(), It.IsAny<Metadata>(),
                        It.IsAny<DateTime?>(),
                        CancellationToken.None)).Returns(handshakeCall);

                    var peer = GrpcTestPeerHelper.CreatePeerWithClient(NetworkTestConstants.GoodPeerEndpoint,
                        NetworkTestConstants.FakePubkey, mockClient.Object);

                    return peer;
                });

            // This peer will pass all checks with success.
            mockDialer.Setup(d =>
                    d.DialPeerAsync(It.Is<DnsEndPoint>(endPoint =>
                        endPoint.ToString() == NetworkTestConstants.GoodPeerEndpoint)))
                .Returns<DnsEndPoint>(s =>
                {
                    var keyPair = CryptoHelper.GenerateKeyPair();
                    var handshakeReply = new HandshakeReply
                    {
                        Handshake = NetworkTestHelper.CreateValidHandshake(keyPair, 10)
                    };
                    var handshakeCall = TestCalls.AsyncUnaryCall(Task.FromResult(handshakeReply),
                        Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(),
                        () => { });
                    var confirmHandshakeCall = TestCalls.AsyncUnaryCall(Task.FromResult(new VoidReply()),
                        Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(),
                        () => { });

                    var mockClient = new Mock<PeerService.PeerServiceClient>();
                    mockClient.Setup(m => m.DoHandshakeAsync(It.IsAny<HandshakeRequest>(), It.IsAny<Metadata>(),
                        It.IsAny<DateTime?>(),
                        CancellationToken.None)).Returns(handshakeCall);

                    mockClient.Setup(m => m.ConfirmHandshakeAsync(It.IsAny<ConfirmHandshakeRequest>(),
                        It.IsAny<Metadata>(), It.IsAny<DateTime?>(),
                        CancellationToken.None)).Returns(confirmHandshakeCall);

                    var peer = GrpcTestPeerHelper.CreatePeerWithClient(NetworkTestConstants.GoodPeerEndpoint,
                        NetworkTestConstants.FakePubkey, mockClient.Object);
                    peer.UpdateLastSentHandshake(handshakeReply.Handshake);

                    return Task.FromResult(peer);
                });

            mockDialer.Setup(d => d.DialBackPeerAsync(It.IsAny<DnsEndPoint>(), It.IsAny<Handshake>()))
                .Returns<DnsEndPoint, Handshake>((endPoint, handshake) =>
                {
                    if (endPoint.ToString() == NetworkTestConstants.GoodPeerEndpoint)
                    {
                        var peer = GrpcTestPeerHelper.CreateBasicPeer(NetworkTestConstants.GoodPeerEndpoint,
                            NetworkTestConstants.FakePubkey);
                        return Task.FromResult(peer);
                    }

                    return Task.FromResult<GrpcPeer>(null);
                });

            return mockDialer.Object;
        });
    }
}

[DependsOn(typeof(GrpcNetworkTestModule))]
public class GrpcNetworkWithBootNodesTestModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        Configure<NetworkOptions>(o =>
        {
            o.ListeningPort = 2001;
            o.MaxPeers = 2;
            o.BootNodes = new List<string>
            {
                "127.0.0.1:2018",
                "127.0.0.1:2019",
                "127.0.0.1:2020"
            };
        });

        services.AddTransient(provider =>
        {
            var mockService = new Mock<IConnectionService>();
            mockService.Setup(m => m.ConnectAsync(It.IsAny<DnsEndPoint>())).Returns<DnsEndPoint>(endpoint =>
            {
                if (endpoint.Port == 2018)
                    return Task.FromResult(false);
                return Task.FromResult(true);
            });
            mockService.Setup(m => m.DisconnectAsync(It.IsAny<IPeer>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var reconnectionService = services.GetRequiredServiceLazy<IReconnectionService>().Value;
            mockService.Setup(m => m.SchedulePeerReconnection(It.IsAny<DnsEndPoint>()))
                .Returns<DnsEndPoint>(endpoint =>
                    Task.FromResult(reconnectionService.SchedulePeerForReconnection(endpoint.ToString())));

            return mockService.Object;
        });
    }
}