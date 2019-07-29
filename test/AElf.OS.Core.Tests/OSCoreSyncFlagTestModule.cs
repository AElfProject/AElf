using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Modularity;
using AElf.OS.Network;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(typeof(OSTestBaseAElfModule))]
    public class OSCoreSyncFlagTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<NetworkOptions>(o => { o.InitialSyncOffset = 5; });

            
            context.Services.AddSingleton<IPeerPool>(o =>
            {
                var peerList = new List<IPeer>();
                
                var peerPoolMock = new Mock<IPeerPool>();
                peerPoolMock.Setup(p => p.GetPeers(It.IsAny<bool>()))
                    .Returns(peerList);
                peerPoolMock.Setup(p => p.TryAddPeer(It.IsAny<IPeer>()))
                    .Callback<IPeer>(peer => peerList.Add(peer));
                
                return peerPoolMock.Object;
            });
            
            context.Services.AddTransient(provider =>
            {
                var mockBlockChainService = new Mock<IBlockchainService>();
                mockBlockChainService.Setup(m => m.GetChainAsync()).Returns(() =>
                {
                    var chain = new Chain();
                    chain.LastIrreversibleBlockHeight = 10;
                    return Task.FromResult(chain);
                });
                return mockBlockChainService.Object;
            });
        }
    }
}