using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Consensus.AEDPoS
{
    [DependsOn(typeof(EconomicContractsTestModule))]
    public class AEDPoSContractTestAElfModule : EconomicContractsTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}