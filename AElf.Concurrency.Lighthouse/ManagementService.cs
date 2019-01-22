using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Microsoft.Extensions.Options;

namespace AElf.Concurrency.Lighthouse
{
    public class ManagementService
    {
        private ActorSystem _actorSystem;
        public Task TerminationHandle => _actorSystem.WhenTerminated;

        private readonly ExecutionOptions _executionOptions;

        public ManagementService(IOptionsSnapshot<ExecutionOptions> options)
        {
            _executionOptions = options.Value;
        }

        private ActorSystem CreateActorSystem()
        {
            var clusterConfig = ConfigurationFactory.ParseString(File.ReadAllText("akka-lighthouse.hocon"));
            var systemName = clusterConfig.GetConfig("manager").GetString("system-name");
            var hostName = _executionOptions.HostName;
            var port = _executionOptions.Port;
            var selfAddress = $"akka.tcp://{systemName}@{hostName}:{port}";

            var seeds = clusterConfig.GetStringList("akka.cluster.seed-nodes");
            if (!seeds.Contains(selfAddress))
                seeds.Add(selfAddress);

            var seedConfigString = seeds.Aggregate("akka.cluster.seed-nodes = [",
                (current, seed) => current + @"""" + seed + @""", ");
            seedConfigString += "]";
            
            var finalConfig = ConfigurationFactory.ParseString(seedConfigString)
                .WithFallback(ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.hostname=" + hostName))
                .WithFallback(ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.port=" + port))
                .WithFallback(clusterConfig);
            return ActorSystem.Create(systemName, finalConfig);
        }

        public void StartSeedNodes()
        {
            _actorSystem = CreateActorSystem();
        }

        public async Task StopAsync()
        {
            await CoordinatedShutdown.Get(_actorSystem).Run(CoordinatedShutdown.ClusterLeavingReason.Instance);
        }
    }
}