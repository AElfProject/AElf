using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
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
                var endpoint = IpEndPointHelper.Parse("127.0.0.1:5000");
            
                var peer = new Mock<IPeer>();
                peer.Setup(p => p.RemoteEndpoint).Returns(endpoint);
                peer.Setup(p => p.Info).Returns(new PeerConnectionInfo {Pubkey = "blacklistpeer", ConnectionTime = TimestampHelper.GetUtcNow()});
                return peer.Object;
            });

            peerPoolMock.Setup(p => p.FindPeerByPublicKey(It.Is<string>(adr => adr == "p1")))
                .Returns<string>(adr =>
                {
                    var p1 = new Mock<IPeer>();
                    
                    var blockWithTransactions = osTestHelper.Value.GenerateBlockWithTransactions(Hash.Empty, 10);

                    p1.Setup(p => p.Info).Returns(new PeerConnectionInfo
                        {Pubkey = "p1", ConnectionTime = TimestampHelper.GetUtcNow()});
                    p1.Setup(p => p.GetBlocksAsync(It.IsAny<Hash>(), It.IsAny<int>()))
                        .Returns<Hash, int>((h, cnt) => Task.FromResult(new List<BlockWithTransactions>()));
                    
                    p1.Setup(p => p.GetBlockByHashAsync(It.Is<Hash>(h => h == Hash.FromString("bHash1"))))
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
                    
                    var blockWithTransactions = osTestHelper.Value.GenerateBlockWithTransactions(Hash.Empty, 10);

                    var p2 = new Mock<IPeer>();
                    p2.Setup(p => p.RemoteEndpoint).Returns(new IPEndPoint(100, 100));
                    p2.Setup(p => p.Info).Returns(new PeerConnectionInfo
                        {Pubkey = "p2", ConnectionTime = TimestampHelper.GetUtcNow()});
                    p2.Setup(p => p.GetBlocksAsync(It.Is<Hash>(h => h == Hash.FromString("block")), It.IsAny<int>()))
                        .Returns<Hash, int>((h, cnt) => Task.FromResult(new List<BlockWithTransactions> { blockWithTransactions }));
                    p2.Setup(p => p.GetBlockByHashAsync(It.Is<Hash>(h => h == Hash.FromString("block"))))
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

                    p3.Setup(p => p.RemoteEndpoint).Returns(new IPEndPoint(100, 100));
                    p3.Setup(p => p.Info).Returns(new PeerConnectionInfo
                        {Pubkey = "p3", ConnectionTime = TimestampHelper.GetUtcNow()});
                    p3.Setup(p => p.GetBlocksAsync(It.Is<Hash>(h => h == Hash.FromString("blocks")), It.IsAny<int>()))
                        .Returns<Hash, int>((h, cnt) => Task.FromResult(new List<BlockWithTransactions> { blockWithTransactions, blockWithTransactions }));
                    p3.Setup(p => p.GetBlockByHashAsync(It.Is<Hash>(h => h == Hash.FromString("bHash2"))))
                        .Returns<Hash>(h => Task.FromResult(blockWithTransactions));
                    peers.Add(p3.Object);
                    
                    var exceptionOnBcast = new Mock<IPeer>();
                    exceptionOnBcast.Setup(p => p.RemoteEndpoint).Returns(new IPEndPoint(100, 100));
                    exceptionOnBcast.Setup(p => p.Info).Returns(new PeerConnectionInfo
                        {Pubkey = "exceptionOnBcast", ConnectionTime = TimestampHelper.GetUtcNow()});
                    
                    peers.Add(exceptionOnBcast.Object);

                    if (includeFailing)
                    {
                        var failingPeer = new Mock<IPeer>();
                        failingPeer.Setup(p => p.RemoteEndpoint).Returns(new IPEndPoint(100, 100));
                        failingPeer.Setup(p => p.Info).Returns(new PeerConnectionInfo
                            {Pubkey = "failing", ConnectionTime = TimestampHelper.GetUtcNow()});
                        peers.Add(failingPeer.Object);
                    }
                    
                    return peers;
                });
            
            context.Services.AddSingleton<IPeerPool>(o => peerPoolMock.Object);

            context.Services.AddTransient(o => Mock.Of<IBroadcastPrivilegedPubkeyListProvider>());
        }
    }

    [DependsOn(typeof(NetworkServiceTestModule))]
    public class NetworkServicePropagationTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            NetworkServicePropagationTestContext testContext = new NetworkServicePropagationTestContext();

            Mock<IPeerPool> peerPoolMock = new Mock<IPeerPool>();
            List<IPeer> peers = null;

            var previousBlockHashes = new List<Hash>();
            peerPoolMock.Setup(p => p.GetPeers(It.IsAny<bool>())).Returns<bool>(adr =>
            {
                if (peers != null)
                    return peers;

                peers = new List<IPeer>();
                
                var propPeerOne = new Mock<IPeer>();
//                propPeerOne.Setup(p => p.RemoteEndpoint).Returns(new IPEndPoint(200, 200));
//                propPeerOne.Setup(p => p.Info).Returns(new PeerConnectionInfo
//                    {Pubkey = "prop_peer_1", ConnectionTime = TimestampHelper.GetUtcNow()});
                
                propPeerOne.Setup(p => p.AddKnownBlock(It.IsAny<Hash>())).Returns<Hash>(blockHash =>
                {
                    if (previousBlockHashes.Contains(blockHash))
                        return false;

                    previousBlockHashes.Add(blockHash);
                    return true;
                });
                
                var previousTransactionHashes = new List<Hash>();
                propPeerOne.Setup(p => p.AddKnownTransaction(It.IsAny<Hash>())).Returns<Hash>(blockHash =>
                {
                    if (previousTransactionHashes.Contains(blockHash))
                        return false;

                    previousTransactionHashes.Add(blockHash);
                    return true;
                });

//                propPeerOne.Setup(p =>
//                    p.EnqueueBlock(It.IsAny<BlockWithTransactions>(), It.IsAny<Action<NetworkException>>())).Callback<BlockWithTransactions, Action<NetworkException>>(
//                    (block, ex) =>
//                    {
//                        ;
//                        Console.WriteLine();
//                    });

//                propPeerOne.Setup(p => p.GetBlocksAsync(It.Is<Hash>(h => h == Hash.FromString("block")), It.IsAny<int>()))
//                    .Returns<Hash, int>((h, cnt) => Task.FromResult(new List<BlockWithTransactions> { blockWithTransactions }));
//                propPeerOne.Setup(p => p.GetBlockByHashAsync(It.Is<Hash>(h => h == Hash.FromString("block"))))
//                    .Returns<Hash>(h => Task.FromResult(blockWithTransactions));
                
                peers.Add(propPeerOne.Object);
                testContext.MockedPeers.Add(propPeerOne);

                return peers;
            });

            context.Services.AddSingleton<IPeerPool>(o => peerPoolMock.Object);
            context.Services.AddSingleton<NetworkServicePropagationTestContext>(o => testContext);
            context.Services.AddTransient(o => Mock.Of<IBroadcastPrivilegedPubkeyListProvider>());

        }
    }

    public class NetworkServicePropagationTestContext
    {
        public List<Mock<IPeer>> MockedPeers = new List<Mock<IPeer>>();
    }
}