using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.FeatureManager;

public class FeatureDisableAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<DisableFeatureOptions>(context.Services.GetConfiguration().GetSection("DisableFeature"));
    }
}