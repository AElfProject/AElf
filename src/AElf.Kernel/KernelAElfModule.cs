using AElf.Kernel.ChainController;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Node;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool;
using AElf.Modularity;
using Volo.Abp;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;


namespace AElf.Kernel
{
    [DependsOn(
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
        
        public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
        {
            var blockTransactionLimitProvider = context.ServiceProvider.GetService<IBlockTransactionLimitProvider>();
            AsyncHelper.RunSync(() => blockTransactionLimitProvider.InitAsync());
        }
    }
}