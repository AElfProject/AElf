using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.DPoS;
using AElf.Modularity;
using AElf.OS.Network;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS.Consensus.DPos
{
    [DependsOn(
        typeof(OSAElfModule),
        typeof(OSCoreWithChainTestAElfModule)
    )]
    public class OSConsensusDPosTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            var peerList = new List<IPeer>();
            for (int i = 0; i < 3; i++)
            {
                var peer = new GrpcPeer(null, null, $"bp{i + 1}-pubkey", $"127.0.0.1:68{i + 1}0");
                peerList.Add(peer);
            }
            
            services.AddTransient(o =>
            {
                var mockService = new Mock<IPeerPool>();
                mockService.Setup(m=>m.FindPeerByPublicKey(It.Is<string>(s => s.Length > 0)))
                    .Returns(peerList[2]);
                mockService.Setup(m=>m.GetPeers(It.IsAny<bool>()))
                    .Returns(peerList);
                return mockService.Object;
            });

            services.AddTransient(o =>
            {
                var mockService = new Mock<IDPoSInformationProvider>();
                mockService.Setup(m=>m.GetCurrentMiners(It.IsAny<ChainContext>()))
                    .Returns(async ()=>
                        await Task.FromResult(new []{
                            "bp1-pubkey",
                            "bp2-pubkey",
                            "bp3-pubkey"
                        }));
                return mockService.Object;

            });
        }
    }
}