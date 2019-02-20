using AElf.Modularity;
using AElf.OS;
using AElf.RPC;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Net.Rpc
{
    [DependsOn(
        typeof(RpcAElfModule),
        typeof(CoreOSAElfModule)
        )]
    public class NetRpcAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<NetRpcAElfModule>();
            context.Services.AddSingleton<NetRpcService>();
        }
    }
}