using System;
using System.IO;
using AElf.ChainController;
using AElf.Common;
using AElf.Configuration.Config.Contract;
using AElf.Database;
using AElf.Execution;
using AElf.Execution.Execution;
using AElf.Kernel;
using AElf.Miner;
using AElf.Network;
using AElf.Runtime.CSharp;
using AElf.SmartContract;
using Akka.Remote;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp;

namespace AElf.Concurrency.Worker
{
    class Program
    {
        private static ILogger<Program> Logger = NullLogger<Program>.Instance;

        static void Main(string[] args)
        {
            //TODO! use abp bootstrap


            var confParser = new ConfigParser();
            bool parsed;
            try
            {
                parsed = confParser.Parse(args);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception while parse config.");
                throw;
            }

            if (!parsed)
                return;


            using (var application = AbpApplicationFactory.Create<WorkerConcurrencyAElfModule>(options =>
            {
                options.UseAutofac();

                options.Services.AddLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Trace)
                        .AddConsole()
                        .AddFile();
                });
            }))
            {
                application.Initialize();

                Logger = application.ServiceProvider.GetRequiredService<ILogger<Program>>();
                
                var service = application.ServiceProvider.GetRequiredService<ActorEnvironment>();
                service.InitWorkActorSystem();
                Console.WriteLine("Press Control + C to terminate.");
                Console.CancelKeyPress += async (sender, eventArgs) => { await service.StopAsync(); };
                service.TerminationHandle.Wait();
            }

        }


        private static bool CheckDBConnect(IServiceProvider sp)
        {
            var db = sp.GetRequiredService<IKeyValueDatabase>();
            try
            {
                return db.IsConnected();
            }
            catch (Exception e)
            {
                //Logger.LogError(e);
                return false;
            }
        }
    }
}