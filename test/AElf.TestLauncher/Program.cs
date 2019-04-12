using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Extensions.Logging.File;

namespace AElf.TestLauncher
{
    class Program
    {
        public static void Main(string[] args)
        {
            ILogger<Program> logger = NullLogger<Program>.Instance;

            try
            {
                Task.WaitAll(RunServers().ToArray());
            }
            catch (Exception e)
            {
                if (logger == NullLogger<Program>.Instance)
                    Console.WriteLine(e);
                logger.LogCritical(e, "program crashed");
            }
        }

        public static IEnumerable<Task> RunServers()
        {
            foreach (var file in Directory.GetFiles("nodes", "*.json").AsParallel())
            {
                Console.WriteLine($"start {file}");

                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);

                yield return new WebHostBuilder()
                    .ConfigureAppConfiguration(config =>
                    {
                        config.AddJsonFile("appsettings.json");
                        config.AddJsonFile(file);
                    })
                    .UseKestrel((builderContext, options) =>
                    {
                        options.Configure(builderContext.Configuration.GetSection("Kestrel"));
                    })
                    .ConfigureLogging(logger =>
                    {
                        logger
                            .AddFile($"Logs/{fileNameWithoutExtension}.log",LogLevel.Trace);
                    })

                    //.UseContentRoot(dir)
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<MainBlockchainStartup<MainBlockchainAElfModule>>()
                    .ConfigureServices(services => { })
                    .Build().RunAsync();
            }
        }
    }
}