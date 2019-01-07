using AElf.Kernel;
using AElf.Modularity;
using AElf.TestBase.Contract;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.SideChain.Tests
{
    [DependsOn(
        typeof(AElf.ChainController.ChainControllerAElfModule),
        typeof(AElf.SmartContract.SmartContractAElfModule),
        typeof(AElf.Runtime.CSharp.CSharpRuntimeAElfModule),
        typeof(AElf.TestBase.Contract.ContractTestAElfModule),
        typeof(KernelAElfModule)
    )]
    public class SideChainContractTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<SideChainContractTestAElfModule>();
        }
    }
}