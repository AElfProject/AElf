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

                var dir = file.Substring(0, file.Length - ".json".Length);

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

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
                    .ConfigureLogging(logger => { })
                    //.UseContentRoot(dir)
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<MainBlockchainStartup>()
                    .ConfigureServices(services => { })
                    .Build().RunAsync();
            }
        }
    }
}