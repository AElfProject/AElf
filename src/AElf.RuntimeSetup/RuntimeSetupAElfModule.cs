using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Modularity;

namespace AElf.RuntimeSetup;

[DependsOn(
    typeof(CoreAElfModule)
)]
public class RuntimeSetupAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddLogging(builder =>
        {
            builder.AddConfiguration(context.Services.GetConfiguration().GetSection("Logging"));

            builder.AddLog4Net();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
    }
}