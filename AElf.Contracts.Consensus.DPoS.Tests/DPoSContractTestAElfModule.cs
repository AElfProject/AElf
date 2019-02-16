using AElf.Contracts.TestBase;
using AElf.Execution;
using AElf.Kernel;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Consensus.DPoS.Tests
{
    [DependsOn(
        typeof(ChainController.ChainControllerAElfModule),
        typeof(SmartContract.SmartContractAElfModule),
        typeof(Runtime.CSharp.CSharpRuntimeAElfModule),
        typeof(ContractTestAElfModule),
        typeof(ExecutionAElfModule),
        typeof(KernelAElfModule)
    )]
    // ReSharper disable once InconsistentNaming
    public class DPoSContractTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<DPoSContractTestAElfModule>();
        }
    }
}