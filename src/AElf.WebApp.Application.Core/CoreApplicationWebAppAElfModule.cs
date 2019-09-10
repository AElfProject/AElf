using AElf.Modularity;
using Volo.Abp.Application;
using Volo.Abp.Modularity;

namespace AElf.WebApp.Application
{
    #pragma warning disable 1591
    [DependsOn(typeof(CoreAElfModule), typeof(AbpDddApplicationModule))]
    public class CoreApplicationWebAppAElfModule : AElfModule
    {
    }
}