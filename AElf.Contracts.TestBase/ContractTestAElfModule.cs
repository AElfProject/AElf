using AElf.Database;
using AElf.Execution;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.TestBase
{
    [DependsOn(
        typeof(AElf.ChainController.ChainControllerAElfModule),
        typeof(AElf.SmartContract.SmartContractAElfModule),
        typeof(AElf.Runtime.CSharp.CSharpRuntimeAElfModule),
        typeof(ExecutionAElfModule),
        typeof(KernelAElfModule),
        typeof(DatabaseAElfModule)
    )]
    public class ContractTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<ContractTestAElfModule>();
            
            context.Services.AddKeyValueDbContext<BlockChainKeyValueDbContext>(o=>o.UseInMemoryDatabase());
            context.Services.AddKeyValueDbContext<StateKeyValueDbContext>(o=>o.UseInMemoryDatabase());
        }
    }
}