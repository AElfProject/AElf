using System;
using System.IO;
using AElf.ChainController;
using AElf.Common;
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
            using (var application = AbpApplicationFactory.Create<WorkerConcurrencyAElfModule>(options =>
            {
                options.UseAutofac();
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
    }
}