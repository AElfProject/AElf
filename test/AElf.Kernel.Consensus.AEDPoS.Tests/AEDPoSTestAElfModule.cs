using AElf.Kernel.Consensus.AEDPoS;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus.DPoS.Tests
{
    [DependsOn(
        typeof(AEDPoSAElfModule))]
    public class AEDPoSTestAElfModule : AElfModule
    {
        
    }
}