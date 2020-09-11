using System.Collections.Generic;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.EconomicSystem.Tests
{
    [DependsOn(
        typeof(EconomicSystemAElfModule)
    )]
    public class EconomicSystemTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ConsensusOptions>(options =>
            {
                options.MiningInterval = 4000;
                options.InitialMinerList = new List<string>{"5945c176c4269dc2aa7daf7078bc63b952832e880da66e5f2237cdf79bc59c5f"};
            });
        }
    }
}