using AElf.Database;
using AElf.Kernel;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Genesis.Tests
{
    [DependsOn(
        typeof(AElf.ChainController.ChainAElfModule),
        typeof(AElf.SmartContract.SmartContractAElfModule),
        typeof(AElf.Runtime.CSharp.RunnerAElfModule),
        typeof(KernelAElfModule)
        )]
    public class GenesisContractTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<GenesisContractTestAElfModule>();
        }
    }

    public class GenesisContractTestBase : TestBase.AElfIntegratedTest<GenesisContractTestAElfModule>
    {
        protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
        {
            base.SetAbpApplicationCreationOptions(options);
            options.UseInMemoryDatabase();
        }
    }
}