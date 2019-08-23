using AElf.Kernel;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.WebApp.Application.Chain
{
    [DependsOn(typeof(CoreKernelAElfModule), typeof(CoreApplicationWebAppAElfModule))]
    public class ChainApplicationWebAppAElfModule : AElfModule
    {
    }
}