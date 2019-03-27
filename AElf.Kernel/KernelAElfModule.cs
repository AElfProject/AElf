using AElf.Kernel.ChainController;
using AElf.Kernel.Node;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.Token;
using AElf.Kernel.TransactionPool;
using AElf.Modularity;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    [DependsOn(
        typeof(AbpBackgroundJobsModule),
        typeof(CoreKernelAElfModule),
        typeof(ChainControllerAElfModule),
        typeof(SmartContractAElfModule),
        typeof(NodeAElfModule),
        typeof(SmartContractExecutionAElfModule),
        typeof(TransactionPoolAElfModule),
        typeof(TokenKernelAElfModule)
    )]
    public class KernelAElfModule : AElfModule<KernelAElfModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}