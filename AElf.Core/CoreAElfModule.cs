using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf
{
    public class CoreAElfModule : AElfModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddConventionalRegistrar(new AElfDefaultConventionalRegistrar());
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            
        }
    }
}