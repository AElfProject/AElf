using System;
using AElf.Common.Enums;
using AElf.Configuration;
using AElf.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp;


namespace AElf.Launcher
{
    class Program
    {
        static void Main(string[] args)
        {
            ILogger<Program> logger = NullLogger<Program>.Instance;
            try
            {
                Console.WriteLine(string.Join(" ", args));

                var parsed = new CommandLineParser();
                parsed.Parse(args);
                
                using (var application = AbpApplicationFactory.Create<LauncherAElfModule>(options =>
                {
                    options.UseAutofac(); //Autofac integration
                    //options.UseInMemoryDatabase(); // 

                    options.Services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Trace)
                            .AddConsole()
                            .AddFile();
                    });
                        
                    //DatabaseConfig.Instance.Type = DatabaseType.Redis;
                }))
                {
                    
                    

                    application.Initialize();
                    
                    logger = application.ServiceProvider.GetRequiredService<ILogger<Program>>();

                    LauncherAElfModule.Closing.WaitOne();
                }
            }
            catch (Exception e)
            {
                if(logger == NullLogger<Program>.Instance)
                    Console.WriteLine(e);
                logger.LogCritical(e,"program crashed");
            }
        }
    }
}