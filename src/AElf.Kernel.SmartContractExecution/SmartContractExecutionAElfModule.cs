using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodCallThreshold;
using AElf.Kernel.SmartContract.ExecutionPluginForResourceFee;
using AElf.Kernel.SmartContract.ExecutionPluginForProposal;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContractExecution
{
    [DependsOn(typeof(SmartContractAElfModule),
        typeof(ExecutionPluginForResourceFeeModule),
        typeof(ExecutionPluginForMethodCallThresholdModule),
        typeof(ExecutionPluginForProposalModule),
        typeof(ExecutionPluginForMethodFeeModule))]
    public class SmartContractExecutionAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IBlockAcceptedLogEventHandler, ContractDeployedLogEventHandler>();
            context.Services.AddSingleton<IBlockAcceptedLogEventHandler, CodeUpdatedLogEventHandler>();
            context.Services.AddSingleton<IBestChainFoundLogEventHandler, CodeCheckRequiredLogEventHandler>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            //var executorType = context.ServiceProvider.GetService<IOptionsSnapshot<ExecutionOptions>>().Value.ExecutorType;
        }
    }
}