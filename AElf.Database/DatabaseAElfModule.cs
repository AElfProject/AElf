using System.Dynamic;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Database
{
    [DependsOn(typeof(CoreAElfModule),typeof(Volo.Abp.Data.AbpDataModule))]
    public class DatabaseAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
        }
    }
}