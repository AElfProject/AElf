using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.TestKit;
using AElf.Kernel.Blockchain.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Election
{
    [DependsOn(typeof(EconomicContractsTestModule))]
    public class ElectionContractTestModule : EconomicContractsTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}