using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Modularity;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
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
            p3.Setup(p => p.Info).Returns(new PeerInfo { Pubkey = "pBestPeer" });
            
            var recentBlockHeightAndHashMappings = new ConcurrentDictionary<long, Hash>();
            
            var osTestHelper = context.Services.GetServiceLazy<OSTestHelper>();

            peerPoolMock.Setup(p => p.FindPeerByPublicKey(It.Is<string>(adr => adr == "p1")))
                .Returns<string>(adr =>
                {
                    var p1 = new Mock<IPeer>();
                    
                    var blockWithTransactions = osTestHelper.Value.GenerateBlockWithTransactions(Hash.Empty, 10);

                    p1.Setup(p => p.Info).Returns(new PeerInfo { Pubkey = "p1" });
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
                    p1.Setup(p => p.Info).Returns(new PeerInfo { Pubkey = "p1" });
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
                    p2.Setup(p => p.Info).Returns(new PeerInfo { Pubkey = "p2" });
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
                    
                    
                    p3.SetupProperty(p => p.IsBest, true);
                    p3.Setup(p => p.RemoteEndpoint).Returns(new IPEndPoint(100, 100));
                    p3.Setup(p => p.Info).Returns(new PeerInfo { Pubkey = "p3" });
                    p3.Setup(p => p.GetBlocksAsync(It.Is<Hash>(h => h == Hash.FromString("blocks")), It.IsAny<int>()))
                        .Returns<Hash, int>((h, cnt) => Task.FromResult(new List<BlockWithTransactions> { blockWithTransactions, blockWithTransactions }));
                    p3.Setup(p => p.GetBlockByHashAsync(It.Is<Hash>(h => h == Hash.FromString("bHash2"))))
                        .Returns<Hash>(h => Task.FromResult(blockWithTransactions));
                    peers.Add(p3.Object);
                    
                    var exceptionOnBcast = new Mock<IPeer>();
                    exceptionOnBcast.Setup(p => p.RemoteEndpoint).Returns(new IPEndPoint(100, 100));
                    exceptionOnBcast.Setup(p => p.Info).Returns(new PeerInfo { Pubkey = "exceptionOnBcast" });
                    
                    peers.Add(exceptionOnBcast.Object);

                    if (includeFailing)
                    {
                        var failingPeer = new Mock<IPeer>();
                        failingPeer.Setup(p => p.RemoteEndpoint).Returns(new IPEndPoint(100, 100));
                        failingPeer.Setup(p => p.Info).Returns(new PeerInfo {Pubkey = "failing"});
                        peers.Add(failingPeer.Object);
                    }
                    
                    return peers;
                });
            
            context.Services.AddSingleton<IPeerPool>(o => peerPoolMock.Object);
        }
    }
}