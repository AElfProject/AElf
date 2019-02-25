using AElf.Kernel.ChainController;
using AElf.Kernel.TransactionPool;
using AElf.Modularity;
using AElf.OS.Rpc;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.ChainController.Rpc
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
            context.Services.AddAssemblyOf<ChainControllerRpcModule>();

            context.Services.AddSingleton<ChainControllerRpcService>();
        }
    }
}