using AElf.Database;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.TestBase
{
    [DependsOn(
        typeof(Kernel.ChainController.ChainControllerAElfModule),
        typeof(Kernel.SmartContract.SmartContractAElfModule),
        typeof(Runtime.CSharp.CSharpRuntimeAElfModule2),
        typeof(SmartContractExecutionAElfModule),
        typeof(CoreKernelAElfModule),
        typeof(DatabaseAElfModule)
    )]
    public class ContractTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<ContractTestAElfModule>();

            context.Services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            context.Services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
        }
    }
}