using AElf.Kernel;
using AElf.Modularity;
using AElf.TestBase.Contract;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Authorization.Tests
{
    [DependsOn(
        typeof(AElf.ChainController.ChainControllerAElfModule),
        typeof(AElf.SmartContract.SmartContractAElfModule),
        typeof(AElf.Runtime.CSharp.CSharpRuntimeAElfModule),
        typeof(KernelAElfModule),
        typeof(ContractTestAElfModule)
        
    )]
    public class AuthroizationContractTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<AuthroizationContractTestAElfModule>();
        }
    }
}