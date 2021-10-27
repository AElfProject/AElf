using AElf.Modularity;
using Volo.Abp.Application;
using Volo.Abp.Modularity;

namespace AElf.WebApp.Application
{
    /// <summary>
    /// Add summary to disable 'Missing XML Comment' warning.
    /// </summary>
    [DependsOn(typeof(CoreAElfModule), typeof(AbpDddApplicationModule))]
    public class CoreApplicationWebAppAElfModule : AElfModule
    {
    }
}