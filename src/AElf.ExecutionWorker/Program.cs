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
                .ConfigureLogging(builder =>
                {
                    builder.ClearProviders();
                })
                .ConfigureServices(s=>s.AddApplication<ExecutionWorkerModule>())
                .UseOrleans(builder =>
                {
                    builder
                        .ConfigureDefaults()
                        .UseLocalhostClustering(primarySiloEndpoint:new IPEndPoint(IPAddress.Loopback,11111))
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "dev";
                            options.ServiceId = "dev";
                        })
                        .Configure<EndpointOptions>(options =>
                        {
                            options.AdvertisedIPAddress = IPAddress.Loopback;
                            options.SiloPort = 11111;
                            options.GatewayPort = 21111;
                        })
                        .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(TransactionExecutingGrain).Assembly).WithReferences())
                        .AddMemoryGrainStorage(name: "ArchiveStorage");
                })
                .UseAutofac();
    }
}