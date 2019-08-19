using AElf.Kernel.ChainController;
using AElf.Kernel.TransactionPool;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Node
{
    [DependsOn(typeof(CoreKernelAElfModule), typeof(TransactionPoolAElfModule), typeof(ChainControllerAElfModule))]
    [Dependency(ServiceLifetime.Singleton, TryRegister = true)]
    public class NodeAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}