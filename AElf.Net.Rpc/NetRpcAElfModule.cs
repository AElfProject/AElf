using AElf.Modularity;
using AElf.RPC;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Net.Rpc
{
    [DependsOn(typeof(RpcAElfModule))]
    public class NetRpcAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<NetRpcAElfModule>();
        }
    }
}