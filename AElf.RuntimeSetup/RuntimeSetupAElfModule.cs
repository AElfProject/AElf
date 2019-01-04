using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using apache.log4net.Extensions.Logging;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Modularity;

namespace AElf.RuntimeSetup
{
    [DependsOn(typeof(CoreAElfModule))]
    public class RuntimeSetupAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {

            context.Services.AddLogging(builder =>
            {
                builder.AddLog4Net(new Log4NetSettings()
                {
                    ConfigFile = Path.GetFullPath(Path.Combine(
                        Path.GetDirectoryName(new Uri(typeof(RuntimeSetupAElfModule).GetTypeInfo().Assembly.CodeBase)
                            .LocalPath) ?? string.Empty, "log4net.config"))
                    //ConfigFile = Path.Combine(Directory.GetCurrentDirectory(),"log4net.config")
                });
                builder.SetMinimumLevel(LogLevel.Trace);
            });
        }
    }
}