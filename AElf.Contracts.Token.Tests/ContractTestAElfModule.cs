using AElf.Kernel;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Token.Tests
{
    [DependsOn(
        typeof(AElf.ChainController.ChainAElfModule),
        typeof(AElf.SmartContract.SmartContractAElfModule),
        typeof(AElf.Runtime.CSharp.RunnerAElfModule),
        typeof(KernelAElfModule)
    )]
    public class ContractTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<ContractTestAElfModule>();
        }
    }
}