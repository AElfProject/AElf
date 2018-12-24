using System;
using AElf.Common.Enums;
using AElf.Configuration;
using AElf.Database;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace AElf.Launcher
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var application = AbpApplicationFactory.Create<LauncherAElfModule>(options =>
            {
                options.UseAutofac(); //Autofac integration
                options.UseRedisDatabase(); // 
                DatabaseConfig.Instance.Type = DatabaseType.Redis;
            }))
            {
                
                Console.WriteLine(string.Join(" ", args));

                var parsed = new CommandLineParser();
                parsed.Parse(args);

                application.Initialize();

                LauncherAElfModule.Closing.WaitOne();
            }
        }
    }
}