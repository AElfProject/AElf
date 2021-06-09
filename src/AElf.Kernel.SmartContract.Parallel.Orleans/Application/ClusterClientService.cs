using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Parallel.Orleans.Application
{
    public class ClusterClientService : IClusterClientService, ISingletonDependency
    {
        public IClusterClient Client { get; }

        public ILogger<ClusterClientService> Logger { get; set; }

        public ClusterClientService(ILoggerProvider loggerProvider, IOptions<ClusterOptions> clusterOptions,
            IOptions<StaticGatewayListProviderOptions> gatewayOptions)
        {
            
            Client = new ClientBuilder()
                .UseStaticClustering(op => { op.Gateways = gatewayOptions.Value.Gateways; })
                .ConfigureServices(services =>
                {
                    services.Configure<ClusterOptions>(o =>
                    {
                        o.ClusterId = clusterOptions.Value.ClusterId;
                        o.ServiceId = clusterOptions.Value.ServiceId;
                    });
                })
                .ConfigureLogging(builder => builder.AddProvider(loggerProvider))
                .Build();
        }

        public Task StartAsync()
        {
            var attempt = 0;
            var maxAttempts = 100;
            var delay = TimeSpan.FromSeconds(1);
            return Client.Connect(async error =>
            {
                if (++attempt < maxAttempts)
                {
                    Logger.LogWarning(error,
                        "Failed to connect to Orleans cluster on attempt {@Attempt} of {@MaxAttempts}.",
                        attempt, maxAttempts);
                    await Task.Delay(delay);
                    return true;
                }
                else
                {
                    Logger.LogError(error,
                        "Failed to connect to Orleans cluster on attempt {@Attempt} of {@MaxAttempts}.",
                        attempt, maxAttempts);

                    return false;
                }
            });
        }

        public async Task StopAsync()
        {
            try
            {
                await Client.Close();
            }
            catch (OrleansException error)
            {
                Logger.LogWarning(error,
                    "Error while gracefully disconnecting from Orleans cluster. Will ignore and continue to shutdown.");
            }
        }
    }
}