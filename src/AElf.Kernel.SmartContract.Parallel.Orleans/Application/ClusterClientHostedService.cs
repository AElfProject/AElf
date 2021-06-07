using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Parallel.Orleans.Application
{
    public class ClusterClientService : IClusterClientService, ISingletonDependency
    {
        public IClusterClient Client { get; }

        public ILogger<ClusterClientService> Logger { get; set; }

        public ClusterClientService(ILoggerProvider loggerProvider)
        {
            Client = new ClientBuilder()
                .UseLocalhostClustering(gatewayPorts: new[] {21111})
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