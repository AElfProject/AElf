using System.Collections.Generic;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Modularity;
using AElf.OS;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.GovernmentSystem.Tests
{
    [DependsOn(
        typeof(GovernmentSystemAElfModule)
    )]
    public class GovernmentSystemTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ConsensusOptions>(options =>
            {
                options.PeriodSeconds = 1200;
                options.MiningInterval = 4000;
                options.InitialMinerList = new List<string>{"5945c176c4269dc2aa7daf7078bc63b952832e880da66e5f2237cdf79bc59c5f"};
            });
            Configure<EconomicOptions>(options =>
            {
                options.MaximumLockTime = 120;
                options.MinimumLockTime = 10;
            });
            context.Services.AddSingleton<IParliamentContractInitializationDataProvider, ParliamentContractInitializationDataProvider>();
        }
    }
}