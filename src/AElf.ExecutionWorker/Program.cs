using System;
using System.Net;
using AElf.Kernel.SmartContract.Parallel.Orleans.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Volo.Abp;

namespace AElf.ExecutionWorker
{
    class Program
    {
        public static void Main(string[] args)
        {
            ILogger<Program> logger = NullLogger<Program>.Instance;
            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {
                if (logger == NullLogger<Program>.Instance)
                    Console.WriteLine(e);
                logger.LogCritical(e, "program crashed");
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(builder => { builder.ClearProviders(); })
                .ConfigureServices(s => s.AddApplication<ExecutionWorkerModule>())
                .UseOrleans((context, builder) =>
                {
                    var invariant = context.Configuration.GetSection("Orleans:Membership:Invariant").Value;
                    var connectionString = context.Configuration.GetSection("Orleans:Membership:ConnectionString").Value;

                    builder
                        .ConfigureDefaults()
                        .UseAdoNetClustering(o =>
                        {
                            o.Invariant = invariant;
                            o.ConnectionString = connectionString;
                        })
                        .ConfigureApplicationParts(parts =>
                            parts.AddApplicationPart(typeof(TransactionExecutingGrain).Assembly).WithReferences());
                })
                .UseAutofac();
    }
}