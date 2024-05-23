using AElf.Kernel.Configuration;
using AElf.Kernel.FeatureDisable.Core;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel.FeatureDisable;

[DependsOn(typeof(FeatureDisableCoreAElfModule),
    typeof(ConfigurationAElfModule))]
public class FeatureDisableAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}