using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.TransactionPool
{
    [DependsOn(
        typeof(TransactionPoolAElfModule),
        typeof(KernelCoreTestAElfModule)
    )]
    public class TransactionPoolTestAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<TxHub>();
        }
    }
    
    [DependsOn(
        typeof(TransactionPoolTestAElfModule),
        typeof(KernelCoreWithChainTestAElfModule)
    )]
    public class TransactionPoolWithChainTestAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}