using AElf.Contracts.TestBase;
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Genesis.Tests
{
    [DependsOn(
        typeof(AElf.ChainController.ChainControllerAElfModule),
        typeof(AElf.SmartContract.SmartContractAElfModule),
        typeof(AElf.Runtime.CSharp.CSharpRuntimeAElfModule),
        typeof(ContractTestAElfModule),
        typeof(KernelAElfModule)
        )]
    public class GenesisContractTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<GenesisContractTestAElfModule>();
        }
    }

    public class GenesisContractTestBase : ContractTestBase<GenesisContractTestAElfModule>
    {
    }
}