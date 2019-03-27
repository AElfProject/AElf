using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus
{
    [DependsOn(typeof(TestBaseKernelAElfModule))]
    public class ConsensusTestAElfModule : AElfModule
    {
    }
}