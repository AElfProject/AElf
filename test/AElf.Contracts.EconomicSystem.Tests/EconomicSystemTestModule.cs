using AElf.Contracts.Economic.TestBase;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.Contracts.EconomicSystem.Tests
{
    [DependsOn(typeof(EconomicContractsTestModule))]
    public class EconomicSystemTestModule : EconomicContractsTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.RemoveAll<IPreExecutionPlugin>();
        }
    }
}