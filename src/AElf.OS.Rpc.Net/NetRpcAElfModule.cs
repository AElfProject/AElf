using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.OS.Rpc.Net
{
    [DependsOn(
        typeof(RpcAElfModule)
    )]
    public class NetRpcAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<NetRpcService>();
        }
    }
}