using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Modularity;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Protocol;
using AElf.Types;
using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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
                var mockBlockchainService = new Mock<IBlockchainService>();
                var keyPair = CryptoHelper.GenerateKeyPair();

                mockBlockchainService.Setup(b => b.GetChainAsync()).ReturnsAsync(new Chain
                {
                    Id = NetworkTestConstants.DefaultChainId
                });

                mockBlockchainService.Setup(b => b.GetBlockHeaderByHashAsync(It.IsAny<Hash>())).ReturnsAsync(
                    netTestHelper.CreateFakeBlockHeader(NetworkTestConstants.DefaultChainId, 1, keyPair));

                return mockBlockchainService.Object;
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
                
                mockDialer.Setup(d => d.DialPeerAsync(It.Is<IPEndPoint>(ip => ip.ToString() == NetworkTestConstants
                .DialExceptionIpEndpoint)))
                    .Throws<Exception>();

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
                        var handshakeReply = new HandshakeReply();
                        
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
                
                // Incorrect handshake signature
                mockDialer.Setup(d => d.DialPeerAsync(It.Is<IPEndPoint>(ip => ip.ToString() == NetworkTestConstants
                .HandshakeWithDataExceptionIp)))
                    .Returns<string>(async (s) =>
                    {
                        var handshakeProvider = context.Services.GetServiceLazy<IHandshakeProvider>().Value;
                        var handshake = await handshakeProvider.GetHandshakeAsync();
                        handshake.HandshakeData.Time = null;
                        var handshakeReply = new HandshakeReply{Handshake = handshake};
                        
                        var handshakeCall = TestCalls.AsyncUnaryCall(Task.FromResult(handshakeReply), 
                            Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });
                            
                        var mockClient = new Mock<PeerService.PeerServiceClient>();
                        mockClient.Setup(m => m.DoHandshakeAsync(It.IsAny<HandshakeRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), 
                            CancellationToken.None)).Returns(handshakeCall);
                        
                        var peer = GrpcTestPeerHelpers.CreatePeerWithClient(NetworkTestConstants.GoodPeerEndpoint,
                            NetworkTestConstants.FakePubkey, mockClient.Object);
                            
                        netTestHelper.AddDialedPeer(peer);
                            
                        return peer;
                    });
                    
                    // This peer will pass all checks with success.
                    mockDialer.Setup(d => d.DialPeerAsync(It.Is<IPEndPoint>(ip => ip.ToString() == NetworkTestConstants.GoodPeerEndpoint)))
                        .Returns<IPEndPoint>(s =>
                        {
                            var keyPair = CryptoHelper.GenerateKeyPair();
                            var handshakeReply = new HandshakeReply {
                                Handshake = netTestHelper.CreateValidHandshake(keyPair, 10)
                            };
                            var handshakeCall = TestCalls.AsyncUnaryCall(Task.FromResult(handshakeReply), 
                                Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });
                            
                            var mockClient = new Mock<PeerService.PeerServiceClient>();
                            mockClient.Setup(m => m.DoHandshakeAsync(It.IsAny<HandshakeRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), 
                                    CancellationToken.None)).Returns(handshakeCall);
                        
                            var peer = GrpcTestPeerHelpers.CreatePeerWithClient(NetworkTestConstants.GoodPeerEndpoint,
                                keyPair.PublicKey.ToHex(), mockClient.Object);
                            
                            netTestHelper.AddDialedPeer(peer);
                            
                            return Task.FromResult(peer);
                    });

                return mockDialer.Object;
            });
        }
    }
}