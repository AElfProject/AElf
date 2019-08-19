using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.WebApp.Application.Net
{
    [DependsOn(typeof(CoreApplicationWebAppAElfModule))]
    [Dependency(ServiceLifetime.Singleton, TryRegister = true)]
    public class NetApplicationWebAppAElfModule : AElfModule
    {
    }
}