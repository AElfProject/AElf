using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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

                yield return new HostBuilder()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .ConfigureAppConfiguration(config =>
                    {
                        config.AddJsonFile("appsettings.json");
                        config.AddJsonFile(file);
                    })
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseKestrel((builderContext, options) =>
                            {
                                options.Configure(builderContext.Configuration.GetSection("Kestrel"));
                            })
                            .UseStartup<MainBlockchainStartup<MainBlockchainAElfModule>>();
                    })
                    .ConfigureLogging(logger =>
                    {
                        logger
                            .AddFile($"Logs/{fileNameWithoutExtension}.log",LogLevel.Trace);
                    })

                    //.UseContentRoot(dir)
                    .ConfigureServices(services => { })
                    .Build().RunAsync();
            }
        }
    }
}