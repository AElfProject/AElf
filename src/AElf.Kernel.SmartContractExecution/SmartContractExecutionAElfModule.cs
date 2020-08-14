using AElf.Kernel.Configuration;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContractExecution
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class SmartContractExecutionAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IBlockAcceptedLogEventProcessor, ContractDeployedLogEventProcessor>();
            context.Services.AddSingleton<IBlockAcceptedLogEventProcessor, CodeUpdatedLogEventProcessor>();
            context.Services.AddTransient(typeof(IConfigurationProcessor),
                typeof(ExecutionObserverThresholdConfigurationProcessor));
            context.Services.AddTransient(typeof(IConfigurationProcessor),
                typeof(StateLimitSizeConfigurationProcessor));
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            //var executorType = context.ServiceProvider.GetService<IOptionsSnapshot<ExecutionOptions>>().Value.ExecutorType;
        }
    }
}