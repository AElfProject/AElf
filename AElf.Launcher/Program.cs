using System;
using AElf.Common.Enums;
using AElf.Configuration;
using AElf.Database;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp;


namespace AElf.Launcher
{
    class Program
    {
        public static void Main(string[] args)
        {
            ILogger<Program> logger = NullLogger<Program>.Instance;
            try
            {
                Console.WriteLine(string.Join(" ", args));

                var parsed = new CommandLineParser();
                parsed.Parse(args);

                CreateWebHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {
                if (logger == NullLogger<Program>.Instance)
                    Console.WriteLine(e);
                logger.LogCritical(e, "program crashed");
            }
        }


        //create default https://github.com/aspnet/MetaPackages/blob/master/src/Microsoft.AspNetCore/WebHost.cs
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging(builder => { builder.ClearProviders(); })
                .ConfigureAppConfiguration(builder => { LauncherAElfModule.Configuration = builder.Build(); })
                .UseStartup<Startup>();
    }
}