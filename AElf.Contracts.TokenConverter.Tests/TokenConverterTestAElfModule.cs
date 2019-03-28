using AElf.Contracts.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.TokenConverter
{
    [DependsOn(typeof(ContractTestAElfModule))]
    public class TokenConverterTestAElfModule : ContractTestAElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<TokenConverterTestAElfModule>();
        }
    }
}