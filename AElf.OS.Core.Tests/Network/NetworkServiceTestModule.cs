using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Modularity;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    public class NetworkServiceTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<INetworkService, NetworkService>();
            
            Mock<IPeerPool> peerPoolMock = new Mock<IPeerPool>();
                
            peerPoolMock.Setup(p => p.FindPeerByPublicKey(It.Is<string>(adr => adr == "p1")))
                .Returns<string>(adr =>
                {
                    var p1 = new Mock<IPeer>();
                    
                    p1.Setup(p => p.GetBlocksAsync(It.IsAny<Hash>(), It.IsAny<int>()))
                        .Returns<Hash, int>((h, cnt) => Task.FromResult(new List<Block>()));
                    
                    p1.Setup(p => p.RequestBlockAsync(It.Is<Hash>(h => h == Hash.FromString("bHash1"))))
                        .Returns<Hash>(h => Task.FromResult(new Block()));
                        
                    return p1.Object;
                });
            
            peerPoolMock.Setup(p => p.FindPeerByPublicKey(It.Is<string>(adr => adr == "failed_peer")))
                .Returns<string>(adr =>
                {
                    var p1 = new Mock<IPeer>();
                    p1.Setup(p => p.RequestBlockAsync(It.IsAny<Hash>())).Throws(new NetworkException());
                    p1.Setup(p => p.GetBlocksAsync(It.IsAny<Hash>(), It.IsAny<int>())).Throws(new NetworkException());
                    return p1.Object;
                });
            
            peerPoolMock.Setup(p => p.GetPeers(It.IsAny<bool>()))
                .Returns<bool>(includeFailing =>
                {
                    List<IPeer> peers = new List<IPeer>();
                    
                    var p2 = new Mock<IPeer>();
                    p2.Setup(p => p.GetBlocksAsync(It.Is<Hash>(h => h == Hash.FromString("block")), It.IsAny<int>()))
                        .Returns<Hash, int>((h, cnt) => Task.FromResult(new List<Block> { new Block() }));
                    
                    p2.Setup(p => p.RequestBlockAsync(It.Is<Hash>(h => h == Hash.FromString("block"))))
                        .Returns<Hash>(h => Task.FromResult(new Block()));
                    peers.Add(p2.Object);
                    
                    var p3 = new Mock<IPeer>();
                    p3.Setup(p => p.GetBlocksAsync(It.Is<Hash>(h => h == Hash.FromString("blocks")), It.IsAny<int>()))
                        .Returns<Hash, int>((h, cnt) => Task.FromResult(new List<Block> { new Block(), new Block() }));
                    
                    p3.Setup(p => p.RequestBlockAsync(It.Is<Hash>(h => h == Hash.FromString("bHash2"))))
                        .Returns<Hash>(h => Task.FromResult(new Block()));
                    peers.Add(p3.Object);
                    
                    var exceptionOnBcast = new Mock<IPeer>();
                    exceptionOnBcast.Setup(p => p.AnnounceAsync(It.IsAny<PeerNewBlockAnnouncement>()))
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
            
            context.Services.AddSingleton<IPeerPool>(o => peerPoolMock.Object);
        }
    }
}