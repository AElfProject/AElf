using AElf.Kernel;
using AElf.Kernel.Consensus.DPoS;
using AElf.Modularity;
using AElf.OS;
using Volo.Abp.Modularity;

namespace AElf.Blockchains.BasicBaseChain
{
    [DependsOn(
        typeof(DPoSConsensusAElfModule),
        typeof(KernelAElfModule),
        typeof(OSAElfModule)
    )]
    public class BasicBaseChainAElfModule : AElfModule<BasicBaseChainAElfModule>
    {
        
    }
}