using System.Collections.Generic;
using AElf.Modularity;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS.Network
{
    [DependsOn(typeof(OSCoreWithChainTestAElfModule))]
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
        }
    }
}