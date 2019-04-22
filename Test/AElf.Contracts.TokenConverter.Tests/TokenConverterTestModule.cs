using AElf.Contracts.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.TokenConverter
{
    [DependsOn(typeof(ContractTestModule))]
    public class TokenConverterTestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<TokenConverterTestModule>();
        }
    }
}