using AElf.Kernel;
using AElf.Modularity;
using AElf.RPC;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.ChainController.Rpc
{
    [DependsOn(typeof(RpcAElfModule),typeof(ChainControllerAElfModule))]
    public class RpcChainControllerAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<RpcChainControllerAElfModule>();

            context.Services.AddTransient<ChainControllerRpcService>();
        }

    }
}