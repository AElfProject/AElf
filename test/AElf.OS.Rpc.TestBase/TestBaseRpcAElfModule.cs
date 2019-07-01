using AElf.Modularity;
using AElf.OS.Rpc.ChainController;
using AElf.OS.Rpc.Net;
using Volo.Abp.AspNetCore.TestBase;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElf.OS.Rpc
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(AbpAspNetCoreTestBaseModule),
        typeof(ChainControllerRpcModule),
        typeof(NetRpcAElfModule),
        //typeof(OSCoreTestAElfModule)
        typeof(OSCoreWithChainTestAElfModule)
    )]
    public class TestBaseRpcAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            
        }
    }
}