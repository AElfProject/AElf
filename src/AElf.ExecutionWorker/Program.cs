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
                    var clusterId = context.Configuration.GetSection("Orleans:Cluster:ClusterId").Value;
                    var serviceId = context.Configuration.GetSection("Orleans:Cluster:ServiceId").Value;
                    var siloPort = Convert.ToInt32(context.Configuration.GetSection("Orleans:Endpoint:SiloPort").Value);
                    var gatewayPort =
                        Convert.ToInt32(context.Configuration.GetSection("Orleans:Endpoint:GatewayPort").Value);
                    var primarySiloPort = Convert.ToInt32(context.Configuration
                        .GetSection("Orleans:ClusterMembership:PrimarySiloPort").Value);

                    // TODO: Use storage clustering.
                    builder
                        .ConfigureDefaults()
                        .UseLocalhostClustering(siloPort, gatewayPort,
                            new IPEndPoint(IPAddress.Loopback, primarySiloPort), serviceId, clusterId)
                        .ConfigureApplicationParts(parts =>
                            parts.AddApplicationPart(typeof(TransactionExecutingGrain).Assembly).WithReferences())
                        .AddMemoryGrainStorage(name: "ArchiveStorage");
                })
                .UseAutofac();
    }
}