using AElf.CLI.JS;
using AElf.CLI.JS.Crypto;
using AElf.CLI.JS.IO;
using AElf.Modularity;
using ChakraCore.NET.Debug;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CLI
{
    public class CliAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddAssemblyOf<CliAElfModule>();

            services.AddTransient<IConsole, Console>();
            services.AddTransient<IJSEngine, JSEngine>();
            services.AddTransient<IRequestExecutor, RequestExecutor>();
            services.AddTransient<IRandomGenerator, PseudoRandomGenerator>();
            services.AddTransient<IDebugAdapter, JSDebugAdapter>();
        }
    }
}