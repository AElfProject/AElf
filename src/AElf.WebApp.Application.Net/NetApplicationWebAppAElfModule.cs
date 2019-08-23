using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.WebApp.Application.Net
{
    [DependsOn(typeof(CoreApplicationWebAppAElfModule))]
    public class NetApplicationWebAppAElfModule : AElfModule
    {
    }
}