using AElf.Modularity;
using AElf.RPC;
using AElf.SmartContract;
using AElf.TxPool;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.ChainController.Rpc
{
    [DependsOn(typeof(RpcAElfModule),typeof(ChainControllerAElfModule),typeof(TxPoolAElfModule)
        )]
    public class RpcChainControllerAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<RpcChainControllerAElfModule>();

            context.Services.AddSingleton<ChainControllerRpcService>();
            
        }

    }
}