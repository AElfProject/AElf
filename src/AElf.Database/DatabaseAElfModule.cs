using System.Dynamic;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Database
{
    [DependsOn(typeof(CoreAElfModule),typeof(Volo.Abp.Data.AbpDataModule))]
    [Dependency(ServiceLifetime.Singleton, TryRegister = true)]
    public class DatabaseAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
        }
    }
}