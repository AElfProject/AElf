using AElf.Kernel.Configuration;
using AElf.Kernel.ChainController;
using AElf.Kernel.Node;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool;
using AElf.Modularity;
using Volo.Abp;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Kernel
{
    [DependsOn(
        typeof(CoreKernelAElfModule),
        typeof(ChainControllerAElfModule),
        typeof(SmartContractAElfModule),
        typeof(NodeAElfModule),
        typeof(SmartContractExecutionAElfModule),
        typeof(TransactionPoolAElfModule),
        typeof(ConfigurationAElfModule)
    )]
    public class KernelAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton(typeof(ILogEventListeningService<>), typeof(LogEventListeningService<>));
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var taskQueueManager = context.ServiceProvider.GetService<ITaskQueueManager>();

            taskQueueManager.CreateQueue(KernelConstants.MergeBlockStateQueueName);
            taskQueueManager.CreateQueue(KernelConstants.ConsensusRequestMiningQueueName);
            taskQueueManager.CreateQueue(KernelConstants.UpdateChainQueueName);
            taskQueueManager.CreateQueue(KernelConstants.ChainCleaningQueueName);
        }
    }
}