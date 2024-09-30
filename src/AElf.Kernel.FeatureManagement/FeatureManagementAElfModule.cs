using AElf.Kernel.Configuration;
using AElf.Kernel.FeatureManagement.Core;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel.FeatureManagement;

[DependsOn(
    typeof(FeatureManagementCoreAElfModule),
    typeof(ConfigurationAElfModule)
)]
public class FeatureManagementAElfModule : AElfModule
{
}