using AElf.Modularity;
using Volo.Abp.Data;
using Volo.Abp.Modularity;

namespace AElf.Database;

[DependsOn(typeof(CoreAElfModule), typeof(AbpDataModule))]
public class DatabaseAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
    }
}