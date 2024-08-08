using AElf.Kernel.Configuration;
using AElf.Kernel.FeatureManager.Core;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel.FeatureManager;

[DependsOn(
    typeof(FeatureManagerCoreAElfModule),
    typeof(ConfigurationAElfModule)
)]
public class FeatureManagerAElfModule : AElfModule
{
}