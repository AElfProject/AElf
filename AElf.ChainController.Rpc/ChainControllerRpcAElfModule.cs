using AElf.Kernel;
using AElf.Modularity;
using AElf.RPC;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.ChainController.Rpc
{
    [DependsOn(typeof(RpcAElfModule))]
    public class ChainControllerRpcAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<ChainControllerRpcAElfModule>();

            context.Services.AddTransient<ChainControllerRpcService>();
        }

    }
}