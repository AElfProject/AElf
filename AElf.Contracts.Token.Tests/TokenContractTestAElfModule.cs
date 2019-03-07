using AElf.Contracts.TestBase;
using AElf.Database;
using AElf.Kernel.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Token
{
    [DependsOn(
        typeof(ContractTestAElfModule)
    )]
    public class TokenContractTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            
        }
    }
}