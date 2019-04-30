using AElf.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Concurrency.Lighthouse
{
    public class LighthouseConcurrencyAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<ExecutionOptions>(configuration.GetSection("Execution"));
            
            context.Services.AddSingleton<ManagementService>();
        }
    }
}