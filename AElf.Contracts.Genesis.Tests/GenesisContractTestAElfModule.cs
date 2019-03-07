using AElf.Contracts.TestBase;
using AElf.Database;
using AElf.Kernel.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Genesis
{
    [DependsOn(
        typeof(ContractTestAElfModule)
    )]
    public class BasicContractZeroTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {

        }
    }
}