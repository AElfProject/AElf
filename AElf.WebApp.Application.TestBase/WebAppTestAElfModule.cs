using AElf.Modularity;
using AElf.OS;
using AElf.WebApp.Web;
using Volo.Abp.AspNetCore.TestBase;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElf.WebApp.Application
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(AbpAspNetCoreTestBaseModule),
        typeof(WebWebAppAElfModule),
        typeof(OSCoreWithChainTestAElfModule)
    )]
    public class WebAppTestAElfModule : AElfModule
    {
    }
}