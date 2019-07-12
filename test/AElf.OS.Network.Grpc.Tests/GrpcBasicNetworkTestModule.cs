using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
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
                o.ListeningPort = 2000;
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
                
                mockDialer.Setup(d => d.DialPeerAsync(It.Is<string>(ip => ip == GrpcTestConstants.FakeIpEndpoint)))
                    .Returns<string>(s =>
                    {
                        var peer = GrpcTestPeerFactory.CreateBasicPeer(GrpcTestConstants.FakeIpEndpoint, GrpcTestConstants.FakePubkey);
                        netTestHelper.AddDialedPeer(peer);
                        return Task.FromResult(peer);
                    });
                
                mockDialer.Setup(d => d.DialPeerAsync(It.Is<string>(ip => ip == GrpcTestConstants.FakeIpEndpoint2)))
                    .Returns<string>(s =>
                    {
                        var peer = GrpcTestPeerFactory.CreateBasicPeer(GrpcTestConstants.FakeIpEndpoint2, GrpcTestConstants.FakePubkey);
                        netTestHelper.AddDialedPeer(peer);
                        return Task.FromResult(peer);
                    });
                
                mockDialer.Setup(d => d.DialPeerAsync(It.Is<string>(ip => ip == GrpcTestConstants.DialExceptionIpEndpoint)))
                    .Throws<PeerDialException>();

                mockDialer.Setup(d => d.DialPeerAsync(It.Is<string>(ip => ip == GrpcTestConstants.HandshakeWithNetExceptionIp)))
                    .Returns<string>(s =>
                    {
                        var mockClient = new Mock<PeerService.PeerServiceClient>();
                        mockClient.Setup(m => m.DoHandshakeAsync(It.IsAny<HandshakeRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), CancellationToken.None))
                            .Throws(new AggregateException());
                        
                        var peer = GrpcTestPeerFactory.CreatePeerWithClient(GrpcTestConstants.FakeIpEndpoint2, GrpcTestConstants.FakePubkey, 
                            mockClient.Object);
                        
                        netTestHelper.AddDialedPeer(peer);
                        
                        return Task.FromResult(peer);
                    });
                
                // Incorrect handshake
                mockDialer.Setup(d => d.DialPeerAsync(It.Is<string>(ip => ip == GrpcTestConstants.BadHandshakeIp)))
                    .Returns<string>(s =>
                    {
                        var handshakeReply = new HandshakeReply {
                            Error = HandshakeError.InvalidHandshake,
                        };
                        
                        var handshakeCall = TestCalls.AsyncUnaryCall(Task.FromResult(handshakeReply), 
                            Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });
                            
                        var mockClient = new Mock<PeerService.PeerServiceClient>();
                        mockClient.Setup(m => m.DoHandshakeAsync(It.IsAny<HandshakeRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), 
                            CancellationToken.None)).Returns(handshakeCall);
                        
                        var peer = GrpcTestPeerFactory.CreatePeerWithClient(GrpcTestConstants.GoodPeerEndpoint,
                            GrpcTestConstants.FakePubkey, mockClient.Object);
                            
                        netTestHelper.AddDialedPeer(peer);
                            
                        return Task.FromResult(peer);
                    });
                    
                    // This peer will pass all checks with success.
                    mockDialer.Setup(d => d.DialPeerAsync(It.Is<string>(ip => ip == GrpcTestConstants.GoodPeerEndpoint)))
                        .Returns<string>(s =>
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
                        
                            var peer = GrpcTestPeerFactory.CreatePeerWithClient(GrpcTestConstants.GoodPeerEndpoint,
                                keypair.PublicKey.ToHex(), mockClient.Object);
                            
                            netTestHelper.AddDialedPeer(peer);
                            
                            return Task.FromResult(peer);
                    });

                return mockDialer.Object;
            });
        }
    }
}