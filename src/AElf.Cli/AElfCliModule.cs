using AElf.Cli.Core;
using AElf.Modularity;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElf.Cli
{
    [DependsOn(
        typeof(AElfCliCoreModule),
        typeof(AbpAutofacModule)
    )]
    public class AElfCliModule : AElfModule
    {

    }
}