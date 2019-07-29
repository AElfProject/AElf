using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Modularity;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
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
                    p2.Setup(p => p.Info).Returns(new PeerInfo { Pubkey = "p2" });
                    p2.Setup(p => p.GetBlocksAsync(It.Is<Hash>(h => h == Hash.FromString("block")), It.IsAny<int>()))
                        .Returns<Hash, int>((h, cnt) => Task.FromResult(new List<BlockWithTransactions> { blockWithTransactions }));
                    p2.Setup(p => p.GetBlockByHashAsync(It.Is<Hash>(h => h == Hash.FromString("block"))))
                        .Returns<Hash>(h => Task.FromResult(blockWithTransactions));
                    peers.Add(p2.Object);
                    
                    
                    p3.SetupProperty(p => p.IsBest, true);
                    p3.Setup(p => p.Info).Returns(new PeerInfo { Pubkey = "p3" });
                    p3.Setup(p => p.GetBlocksAsync(It.Is<Hash>(h => h == Hash.FromString("blocks")), It.IsAny<int>()))
                        .Returns<Hash, int>((h, cnt) => Task.FromResult(new List<BlockWithTransactions> { blockWithTransactions, blockWithTransactions }));
                    p3.Setup(p => p.GetBlockByHashAsync(It.Is<Hash>(h => h == Hash.FromString("bHash2"))))
                        .Returns<Hash>(h => Task.FromResult(blockWithTransactions));
                    peers.Add(p3.Object);
                    
                    var exceptionOnBcast = new Mock<IPeer>();
                    exceptionOnBcast.Setup(p => p.Info).Returns(new PeerInfo { Pubkey = "exceptionOnBcast" });
                    exceptionOnBcast.Setup(p => p.SendAnnouncementAsync(It.IsAny<BlockAnnouncement>()))
                        .Throws(new NetworkException());
                    exceptionOnBcast.Setup(p => p.SendTransactionAsync(It.IsAny<Transaction>()))
                        .Throws(new NetworkException());
                    
                    peers.Add(exceptionOnBcast.Object);

                    if (includeFailing)
                    {
                        var failingPeer = new Mock<IPeer>();
                        peers.Add(failingPeer.Object);
                    }
                    
                    return peers;
                });
            
//            peerPoolMock.Setup(p => p.AddRecentBlockHeightAndHash(It.IsAny<long>(), It.IsAny<Hash>(), It.IsAny<bool>
//                ())).Callback<long, Hash, bool>((blockHeight, blockHash, hasFork) =>
//            {
//                recentBlockHeightAndHashMappings[blockHeight] = blockHash;
//            });
//
//            peerPoolMock.Setup(p => p.RecentBlockHeightAndHashMappings).Returns(recentBlockHeightAndHashMappings);
            
            context.Services.AddSingleton<IPeerPool>(o => peerPoolMock.Object);
        }
    }
}