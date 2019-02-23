using AElf.Modularity;
using AElf.OS;
using AElf.Rpc;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Net.Rpc
{
    [DependsOn(
        typeof(CoreOSAElfModule),
        typeof(RpcAElfModule)
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