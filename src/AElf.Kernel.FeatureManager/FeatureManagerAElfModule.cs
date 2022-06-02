using AElf.Kernel.Configuration;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel.FeatureManager;

[DependsOn(
    typeof(ConfigurationAElfModule)
    )]
public class FeatureManagerAElfModule : AElfModule
{

}
