using AElf.Kernel;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.WebApp.Application.Chain
{
    [DependsOn(typeof(CoreKernelAElfModule), typeof(CoreApplicationWebAppAElfModule))]
    [Dependency(ServiceLifetime.Singleton, TryRegister = true)]
    public class ChainApplicationWebAppAElfModule : AElfModule
    {
    }
}