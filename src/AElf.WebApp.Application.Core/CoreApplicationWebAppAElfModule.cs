using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Application;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.WebApp.Application
{
    [DependsOn(typeof(CoreAElfModule), typeof(AbpDddApplicationModule))]
    [Dependency(ServiceLifetime.Singleton, TryRegister = true)]
    public class CoreApplicationWebAppAElfModule : AElfModule
    {
    }
}