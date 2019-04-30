using System;
using System.IO;
using apache.log4net.Extensions.Logging;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Modularity;
using System.Linq;

namespace AElf.RuntimeSetup
{
    [DependsOn(
        typeof(CoreAElfModule)
    )]
    public class RuntimeSetupAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var variables = Environment.GetEnvironmentVariables();
            var keys = variables.Keys.Cast<string>().Select(p => new {Ori = p, Uni = p.ToUpper()}).GroupBy(p => p.Uni).ToList();
            foreach (var key in keys)
            {
                if (key.Count() <= 1) continue;
                var list = key.ToList();
                for (var i = 1; i < list.Count; i++)
                {
                    Environment.SetEnvironmentVariable(list[i].Ori, null);
                }
            }

            var log4NetConfigFile = Path.Combine(context.Services.GetHostingEnvironment().ContentRootPath, "log4net.config");
            if (!File.Exists(log4NetConfigFile))
            {
                log4NetConfigFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config");
            }

            context.Services.AddLogging(builder =>
            {
                builder.AddLog4Net(new Log4NetSettings()
                {
                    ConfigFile = log4NetConfigFile
                });
                
                builder.SetMinimumLevel(LogLevel.Debug);
            });
        }
    }
}