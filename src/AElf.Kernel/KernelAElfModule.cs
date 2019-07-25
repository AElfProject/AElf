using AElf.Kernel.ChainController;
using AElf.Kernel.Node;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.TransactionPool;
using AElf.Modularity;
using Volo.Abp;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;


namespace AElf.Kernel
{
    [DependsOn(
        typeof(AbpBackgroundJobsModule),
        typeof(CoreKernelAElfModule),
        typeof(ChainControllerAElfModule),
        typeof(SmartContractAElfModule),
        typeof(NodeAElfModule),
        typeof(SmartContractExecutionAElfModule),
        typeof(TransactionPoolAElfModule)
    )]
    public class KernelAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var taskQueueManager = context.ServiceProvider.GetService<ITaskQueueManager>();

            taskQueueManager.CreateQueue(KernelConstants.MergeBlockStateQueueName);
            taskQueueManager.CreateQueue(KernelConstants.ConsensusRequestMiningQueueName);
            taskQueueManager.CreateQueue(KernelConstants.UpdateChainQueueName);


        }
    }
}