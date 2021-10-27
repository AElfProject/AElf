using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContractExecution
{
    [DependsOn(
        typeof(SmartContractExecutionAElfModule),
        typeof(KernelCoreTestAElfModule),
        typeof(SmartContractTestAElfModule)
    )]
    public class SmartContractExecutionTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<SmartContractExecutionHelper>();
            context.Services.AddSingleton<ContractDeployedLogEventProcessor>();
            context.Services.AddSingleton<CodeUpdatedLogEventProcessor>();
            context.Services.Replace(ServiceDescriptor.Singleton<ITransactionExecutingService, PlainTransactionExecutingService>());
        }
    }
}