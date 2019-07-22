using AElf.Kernel.ChainController;
using AElf.Kernel.TransactionPool;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.OS.Rpc.ChainController
{
    [DependsOn(
        typeof(RpcAElfModule),
        typeof(ChainControllerAElfModule),
        typeof(TransactionPoolAElfModule)
    )]
    public class ChainControllerRpcModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<ChainControllerRpcService>();
        }
    }
}