using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract;
using AElf.Modularity;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol.Types;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSubstitute;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(typeof(OSCoreTestAElfModule))]
    public class NetworkServiceTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<INetworkService, NetworkService>();

            Mock<IPeerPool> peerPoolMock = new Mock<IPeerPool>();
            var p3 = new Mock<IPeer>();
            p3.Setup(p => p.Info).Returns(new PeerConnectionInfo { Pubkey = "pBestPeer" });
            
            var osTestHelper = context.Services.GetServiceLazy<OSTestHelper>();
            
            peerPoolMock.Setup(p => p.FindPeerByPublicKey(It.Is<string>(adr => adr == "blacklistpeer")))
                .Returns<string>(adr =>
            {
                AElfPeerEndpointHelper.TryParse("127.0.0.1:5000", out var endpoint);
            
                var peer = new Mock<IPeer>();
                peer.Setup(p => p.RemoteEndpoint).Returns(endpoint);
                peer.Setup(p => p.Info).Returns(new PeerConnectionInfo {Pubkey = "blacklistpeer", ConnectionTime = TimestampHelper.GetUtcNow()});
                return peer.Object;
            });

            peerPoolMock.Setup(p => p.FindPeerByPublicKey(It.Is<string>(adr => adr == "p1")))
                .Returns<string>(adr =>
                {
                    var p1 = new Mock<IPeer>();
                    p1.Setup(m => m.RemoteEndpoint).Returns(new DnsEndPoint("127.0.0.1", 3210));
                    p1.Setup(m => m.Info).Returns(new PeerConnectionInfo
                    {
                        Pubkey = "p1"
                    });
                    var blockWithTransactions = osTestHelper.Value.GenerateBlockWithTransactions(Hash.Empty, 10);

                    p1.Setup(p => p.Info).Returns(new PeerConnectionInfo
                        {Pubkey = "p1", ConnectionTime = TimestampHelper.GetUtcNow()});
                    p1.Setup(p => p.GetBlocksAsync(It.IsAny<Hash>(), It.IsAny<int>()))
                        .Returns<Hash, int>((h, cnt) => Task.FromResult(new List<BlockWithTransactions>()));
                    
                    p1.Setup(p => p.GetBlockByHashAsync(It.Is<Hash>(h => h == HashHelper.ComputeFrom("bHash1"))))
                        .Returns<Hash>(h => Task.FromResult(blockWithTransactions));
                        
                    return p1.Object;
                });
            
            peerPoolMock.Setup(p => p.FindPeerByPublicKey(It.Is<string>(adr => adr == "failed_peer")))
                .Returns<string>(adr =>
                {
                    var p1 = new Mock<IPeer>();
                    p1.Setup(p => p.Info).Returns(new PeerConnectionInfo
                        {Pubkey = "p1", ConnectionTime = TimestampHelper.GetUtcNow()});
                    p1.Setup(p => p.GetBlockByHashAsync(It.IsAny<Hash>())).Throws(new NetworkException());
                    p1.Setup(p => p.GetBlocksAsync(It.IsAny<Hash>(), It.IsAny<int>())).Throws(new NetworkException());
                    return p1.Object;
                });
            
            peerPoolMock.Setup(p => p.GetPeers(It.IsAny<bool>()))
                .Returns<bool>(includeFailing =>
                {
                    List<IPeer> peers = new List<IPeer>();
                    var mockPeerEndpoint = new DnsEndPoint("10.10.10.10", 100);
                    
                    var blockWithTransactions = osTestHelper.Value.GenerateBlockWithTransactions(Hash.Empty, 10);

                    var p2 = new Mock<IPeer>();
                    p2.Setup(p => p.RemoteEndpoint).Returns(mockPeerEndpoint);
                    p2.Setup(p => p.Info).Returns(new PeerConnectionInfo
                        {Pubkey = "p2", ConnectionTime = TimestampHelper.GetUtcNow()});
                    p2.Setup(p => p.GetBlocksAsync(It.Is<Hash>(h => h == HashHelper.ComputeFrom("block")), It.IsAny<int>()))
                        .Returns<Hash, int>((h, cnt) => Task.FromResult(new List<BlockWithTransactions> { blockWithTransactions }));
                    p2.Setup(p => p.GetBlockByHashAsync(It.Is<Hash>(h => h == HashHelper.ComputeFrom("block"))))
                        .Returns<Hash>(h => Task.FromResult(blockWithTransactions));
                    p2.Setup(m => m.GetNodesAsync(It.IsAny<int>()))
                        .Returns(Task.FromResult(new NodeList
                        {
                            Nodes =
                            {
                                new NodeInfo
                                {
                                    Endpoint = "http://127.0.0.1:8000",
                                    Pubkey = ByteString.CopyFromUtf8("p2")
                                }
                            }
                        }));
                    peers.Add(p2.Object);

                    p3.Setup(p => p.RemoteEndpoint).Returns(mockPeerEndpoint);
                    p3.Setup(p => p.Info).Returns(new PeerConnectionInfo
                        {Pubkey = "p3", ConnectionTime = TimestampHelper.GetUtcNow()});
                    p3.Setup(p => p.GetBlocksAsync(It.Is<Hash>(h => h == HashHelper.ComputeFrom("blocks")), It.IsAny<int>()))
                        .Returns<Hash, int>((h, cnt) => Task.FromResult(new List<BlockWithTransactions> { blockWithTransactions, blockWithTransactions }));
                    p3.Setup(p => p.GetBlockByHashAsync(It.Is<Hash>(h => h == HashHelper.ComputeFrom("bHash2"))))
                        .Returns<Hash>(h => Task.FromResult(blockWithTransactions));
                    peers.Add(p3.Object);
                    
                    var exceptionOnBcast = new Mock<IPeer>();
                    exceptionOnBcast.Setup(p => p.RemoteEndpoint).Returns(mockPeerEndpoint);
                    exceptionOnBcast.Setup(p => p.Info).Returns(new PeerConnectionInfo
                        {Pubkey = "exceptionOnBcast", ConnectionTime = TimestampHelper.GetUtcNow()});
                    
                    peers.Add(exceptionOnBcast.Object);

                    if (includeFailing)
                    {
                        var failingPeer = new Mock<IPeer>();
                        failingPeer.Setup(p => p.RemoteEndpoint).Returns(mockPeerEndpoint);
                        failingPeer.Setup(p => p.Info).Returns(new PeerConnectionInfo
                            {Pubkey = "failing", ConnectionTime = TimestampHelper.GetUtcNow()});
                        peers.Add(failingPeer.Object);
                    }
                    
                    return peers;
                });
            peerPoolMock.Setup(p => p.FindPeerByEndpoint(It.IsAny<DnsEndPoint>()))
                .Returns<DnsEndPoint>(dnsEndPoint =>
                {
                    var peer = new Mock<IPeer>();
                    peer.Setup(p => p.RemoteEndpoint).Returns(dnsEndPoint);
                    peer.Setup(p => p.Info).Returns(new PeerConnectionInfo {Pubkey = "pubkey", ConnectionTime = TimestampHelper.GetUtcNow()});
                    return peer.Object;
                });
            
            context.Services.AddSingleton<IPeerPool>(o => peerPoolMock.Object);

            context.Services.AddTransient(o => Mock.Of<IBroadcastPrivilegedPubkeyListProvider>());
        }
    }

    [DependsOn(typeof(NetworkServiceTestModule))]
    public class NetworkServicePropagationTestModule : AElfModule
    {
        private class KnownHashContainer
        {
            private readonly List<Hash> _knownHashes = new List<Hash>();
            public bool TryAdd(Hash hash)
            {
                if (HasHash(hash)) 
                    return false;

                _knownHashes.Add(hash);

                return true;
            }

            public bool HasHash(Hash hash)
            {
                return _knownHashes.Contains(hash);
            }
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            NetworkServicePropagationTestContext testContext = new NetworkServicePropagationTestContext();

            List<IPeer> peers = null;

            var peerPoolMock = new Mock<IPeerPool>();

            peerPoolMock.Setup(p => p.GetPeers(It.IsAny<bool>())).Returns<bool>(adr =>
            {
                if (peers != null)
                    return peers;

                peers = new List<IPeer>();
                for (var i = 0; i < 3; i++)
                {
                    var peer = new Mock<IPeer>();
                    var knownBlockHashes = new KnownHashContainer();
                    var knownTransactionHashes = new KnownHashContainer();

                    peer.Setup(p => p.TryAddKnownBlock(It.IsAny<Hash>()))
                        .Returns<Hash>(blockHash => knownBlockHashes.TryAdd(blockHash));
                    peer.Setup(p => p.KnowsBlock(It.IsAny<Hash>()))
                        .Returns<Hash>(blockHash => knownBlockHashes.HasHash(blockHash));

                    peer.Setup(p => p.TryAddKnownTransaction(It.IsAny<Hash>()))
                        .Returns<Hash>(txHash => knownTransactionHashes.TryAdd(txHash));
                    peer.Setup(p => p.KnowsTransaction(It.IsAny<Hash>()))
                        .Returns<Hash>(txHash => knownTransactionHashes.HasHash(txHash));
                    SetupBroadcastCallbacks(peer);

                    peers.Add(peer.Object);
                    testContext.MockedPeers.Add(peer);
                }
                
                return peers;
            });

            context.Services.AddSingleton<IPeerPool>(o => peerPoolMock.Object);
            context.Services.AddSingleton<NetworkServicePropagationTestContext>(o => testContext);
            context.Services.AddTransient(o => Mock.Of<IBroadcastPrivilegedPubkeyListProvider>());
        }

        private void SetupBroadcastCallbacks(Mock<IPeer> peerMock)
        {
            // set up the mock to execute the broadcast callbacks

            peerMock
                .Setup(p => p.EnqueueBlock(It.IsAny<BlockWithTransactions>(), It.IsAny<Action<NetworkException>>()))
                .Callback<BlockWithTransactions, Action<NetworkException>>((block, action) => action.Invoke(null));
                
            peerMock
                .Setup(p => p.EnqueueAnnouncement(It.IsAny<BlockAnnouncement>(), It.IsAny<Action<NetworkException>>()))
                .Callback<BlockAnnouncement, Action<NetworkException>>((announce, action) => action.Invoke(null));
                
            peerMock
                .Setup(p => p.EnqueueTransaction(It.IsAny<Transaction>(), It.IsAny<Action<NetworkException>>()))
                .Callback<Transaction, Action<NetworkException>>((transaction, action) => action.Invoke(null));
        }
    }

    public class NetworkServicePropagationTestContext
    {
        public List<Mock<IPeer>> MockedPeers = new List<Mock<IPeer>>();
    }
}