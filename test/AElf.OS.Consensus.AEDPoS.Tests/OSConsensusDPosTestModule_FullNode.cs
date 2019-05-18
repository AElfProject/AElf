using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS.Consensus.DPos
{
    
    [DependsOn(
        typeof(OSAElfModule),
        typeof(OSCoreWithChainTestAElfModule),
        typeof(OSConsensusDPosTestModule_BP)
    )]
    // ReSharper disable once InconsistentNaming
    public class OSConsensusDPosTestModule_FullNode : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddTransient(o =>
            {
                var mockService = new Mock<IAEDPoSInformationProvider>();
                mockService.Setup(m=>m.GetCurrentMinerList(It.IsAny<ChainContext>()))
                    .Returns(async ()=>
                        await Task.FromResult(new []{
                            OSConsensusDPosTestConstants.Bp1PublicKey,
                            OSConsensusDPosTestConstants.Bp2PublicKey,
                            OSConsensusDPosTestConstants.Bp3PublicKey,
                        }));
                return mockService.Object;

            });
        }
    }
}