using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf
{
    public class SpecsModule: AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<SpecsModule>();
        }
    }
}