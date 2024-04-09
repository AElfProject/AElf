using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel.FeatureDisable.Tests;

[DependsOn(
    typeof(FeatureDisableAElfModule),
    typeof(TestBaseKernelAElfModule))]
public class FeatureDisableTestModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<DisableFeatureOptions>(o => o.FeatureNameList = new List<string>
        {
            "FeatureA",
            "FeatureB",
            "FeatureBAndC",
        });
    }
}