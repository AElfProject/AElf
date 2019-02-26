using AElf.Database;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Contexts;
using AElf.Modularity;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Sdk.CSharp2.Tests
{
    [DependsOn(
        typeof(DatabaseAElfModule),
        typeof(TestBaseAElfModule),
        typeof(SmartContractAElfModule))]
    public class TestSdkCSharpAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            context.Services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            context.Services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
            services.AddTransient<IStateProviderFactory, StateProviderFactory>();
        }
    }
}