using AElf.Kernel;
using AElf.Kernel.Consensus.DPoS;
using AElf.Modularity;
using Volo.Abp;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;

namespace AElf.OS.Consensus.DPos
{
    [DependsOn(typeof(DPoSConsensusAElfModule)), DependsOn(typeof(CoreOSAElfModule))]
    // ReSharper disable once InconsistentNaming
    public class DPoSConsensusOSAElfModule : AElfModule<DPoSConsensusOSAElfModule>
    {
    }
}