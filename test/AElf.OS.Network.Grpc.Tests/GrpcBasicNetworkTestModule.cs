using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Modularity;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSubstitute;
using Volo.Abp.Modularity;

namespace AElf.OS.Network
{
    [DependsOn(typeof(OSCoreTestAElfModule), typeof(GrpcNetworkModule))]
    public class GrpcBasicNetworkTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var netTestHelper = new NetworkTestContextHelpers();
            context.Services.AddSingleton(netTestHelper);
                
            Configure<NetworkOptions>(o => {
                o.ListeningPort = 2001;
                o.MaxPeers = 2;
            });

            context.Services.AddTransient(o =>
            {
                var mockHandshakeProvider = new Mock<IHandshakeProvider>();
                mockHandshakeProvider.Setup(h => h.GetHandshakeAsync()).ReturnsAsync(new Handshake());
                return mockHandshakeProvider.Object;
            });
            
            context.Services.AddTransient(sp =>
            {
                var mockDialer = new Mock<IPeerDialer>();
                
                mockDialer.Setup(d => d.DialPeerAsync(It.Is<IPEndPoint>(ip => ip.ToString() == NetworkTestConstants.FakeIpEndpoint)))
                    .Returns<IPEndPoint>(s =>
                    {
                        var peer = GrpcTestPeerHelpers.CreateBasicPeer(NetworkTestConstants.FakeIpEndpoint, NetworkTestConstants.FakePubkey);
                        netTestHelper.AddDialedPeer(peer);
                        return Task.FromResult(peer);
                    });
                
                mockDialer.Setup(d => d.DialPeerAsync(It.Is<IPEndPoint>(ip => ip.ToString() == NetworkTestConstants.FakeIpEndpoint2)))
                    .Returns<IPEndPoint>(s =>
                    {
                        var peer = GrpcTestPeerHelpers.CreateBasicPeer(NetworkTestConstants.FakeIpEndpoint2, NetworkTestConstants.FakePubkey);
                        netTestHelper.AddDialedPeer(peer);
                        return Task.FromResult(peer);
                    });
                
                mockDialer.Setup(d => d.DialPeerAsync(It.Is<IPEndPoint>(ip => ip.ToString() == NetworkTestConstants.DialExceptionIpEndpoint)))
                    .Throws<PeerDialException>();

                mockDialer.Setup(d => d.DialPeerAsync(It.Is<IPEndPoint>(ip => ip.ToString() == NetworkTestConstants.HandshakeWithNetExceptionIp)))
                    .Returns<IPEndPoint>(s =>
                    {
                        var mockClient = new Mock<PeerService.PeerServiceClient>();
                        mockClient.Setup(m => m.DoHandshakeAsync(It.IsAny<HandshakeRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), CancellationToken.None))
                            .Throws(new AggregateException());
                        
                        var peer = GrpcTestPeerHelpers.CreatePeerWithClient(NetworkTestConstants.FakeIpEndpoint2, NetworkTestConstants.FakePubkey, 
                            mockClient.Object);
                        
                        netTestHelper.AddDialedPeer(peer);
                        
                        return Task.FromResult(peer);
                    });
                
                // Incorrect handshake
                mockDialer.Setup(d => d.DialPeerAsync(It.Is<IPEndPoint>(ip => ip.ToString() == NetworkTestConstants.BadHandshakeIp)))
                    .Returns<IPEndPoint>((s) =>
                    {
                        var handshakeReply = new HandshakeReply {
                            Error = HandshakeError.InvalidHandshake,
                        };
                        
                        var handshakeCall = TestCalls.AsyncUnaryCall(Task.FromResult(handshakeReply), 
                            Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });
                            
                        var mockClient = new Mock<PeerService.PeerServiceClient>();
                        mockClient.Setup(m => m.DoHandshakeAsync(It.IsAny<HandshakeRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), 
                            CancellationToken.None)).Returns(handshakeCall);
                        
                        var peer = GrpcTestPeerHelpers.CreatePeerWithClient(NetworkTestConstants.GoodPeerEndpoint,
                            NetworkTestConstants.FakePubkey, mockClient.Object);
                            
                        netTestHelper.AddDialedPeer(peer);
                            
                        return Task.FromResult(peer);
                    });
                    
                    // This peer will pass all checks with success.
                    mockDialer.Setup(d => d.DialPeerAsync(It.Is<IPEndPoint>(ip => ip.ToString() == NetworkTestConstants.GoodPeerEndpoint)))
                        .Returns<IPEndPoint>(s =>
                        {
                            var keypair = CryptoHelper.GenerateKeyPair();
                            var handshakeReply = new HandshakeReply {
                                Error = HandshakeError.HandshakeOk,
                                Handshake = netTestHelper.CreateValidHandshake(keypair, 10)
                            };
                            var handshakeCall = TestCalls.AsyncUnaryCall(Task.FromResult(handshakeReply), 
                                Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });
                            
                            var mockClient = new Mock<PeerService.PeerServiceClient>();
                            mockClient.Setup(m => m.DoHandshakeAsync(It.IsAny<HandshakeRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), 
                                    CancellationToken.None)).Returns(handshakeCall);
                        
                            var peer = GrpcTestPeerHelpers.CreatePeerWithClient(NetworkTestConstants.GoodPeerEndpoint,
                                keypair.PublicKey.ToHex(), mockClient.Object);
                            
                            netTestHelper.AddDialedPeer(peer);
                            
                            return Task.FromResult(peer);
                    });

                return mockDialer.Object;
            });
        }
    }

    [DependsOn(typeof(GrpcBasicNetworkTestModule))]
    public class GrpcNetworkWithBootNodesTestModule : AElfModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            Configure<NetworkOptions>(o =>
            {
                o.ListeningPort = 2001;
                o.MaxPeers = 2;
                o.BootNodes = new List<string>
                {
                    "127.0.0.1:2018",
                    "127.0.0.1:2019"
                };
            });
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddTransient(provider =>
            {
                var mockService = new Mock<IConnectionService>();
                mockService.Setup(m => m.ConnectAsync(It.IsAny<IPEndPoint>())).Returns(Task.FromResult(true));
                mockService.Setup(m => m.DisconnectAsync(It.IsAny<IPeer>(), It.IsAny<bool>()))
                    .Returns(Task.CompletedTask);
                
                return mockService.Object;
            });
        }
    }
}