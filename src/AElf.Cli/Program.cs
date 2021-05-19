using System;
using System.IO;
using System.Threading.Tasks;
using AElf.Cli.Core;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Volo.Abp;

namespace AElf.Cli
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
#if DEBUG
                .MinimumLevel.Override("AElf.Cli", LogEventLevel.Debug)
#else
                .MinimumLevel.Override("AElf.Cli", LogEventLevel.Information)
#endif
                .Enrich.FromLogContext()
                .WriteTo.File(Path.Combine(AElfCliPaths.Log, "aelf-cli-logs.txt"))
                .WriteTo.Console()
                .CreateLogger();
            

            using (var application = AbpApplicationFactory.Create<AElfCliModule>(
                options =>
                {
                    options.UseAutofac();
                    options.Services.AddLogging(c => c.AddSerilog());
                }))
            {
                application.Initialize();

                await application.ServiceProvider
                    .GetRequiredService<AElfCliService>()
                    .RunAsync(args);
                
                application.Shutdown();
                
                Log.CloseAndFlush();
            }
        }
    }
}