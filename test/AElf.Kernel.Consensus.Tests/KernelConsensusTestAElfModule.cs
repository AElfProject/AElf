using AElf.Kernel.Consensus.AEDPoS;
using AElf.Modularity;
using AElf.OS;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus
{
    [DependsOn(
        typeof(AEDPoSAElfModule),
        typeof(KernelTestAElfModule),
        typeof(OSCoreWithChainTestAElfModule)
    )]
    public class KernelConsensusTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}