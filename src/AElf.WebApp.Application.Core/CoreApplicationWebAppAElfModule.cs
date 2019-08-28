using AElf.Modularity;
using Volo.Abp.Application;
using Volo.Abp.Modularity;

namespace AElf.WebApp.Application
{
    [DependsOn(typeof(CoreAElfModule), typeof(AbpDddApplicationModule))]
    public class CoreApplicationWebAppAElfModule : AElfModule
    {
    }
}