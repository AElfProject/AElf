using System.Linq;
using System.Threading.Tasks;
using AElf.Configuration;
using Akka.Actor;
using Akka.Configuration;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Host;

namespace AElf.Concurrency.Manager
{
    public class ManagementService
    {
        private ActorSystem _actorSystem;
        public Task TerminationHandle => _actorSystem.WhenTerminated;

        private static ActorSystem CreateActorSystem()
        {
            var clusterConfig = ConfigurationFactory.ParseString(ActorConfig.Instance.ManagerHoconConfig);
            var systemName = clusterConfig.GetConfig("manager").GetString("system-name");
            var ipAddress = clusterConfig.GetConfig("akka.remote").GetString("dot-netty.tcp.hostname");
            var port = clusterConfig.GetConfig("akka.remote").GetString("dot-netty.tcp.port");
            var selfAddress = $"akka.tcp://{systemName}@{ipAddress}:{port}";

            var seeds = clusterConfig.GetStringList("akka.cluster.seed-nodes");
            if (!seeds.Contains(selfAddress))
                seeds.Add(selfAddress);

            var seedConfigString = seeds.Aggregate("akka.cluster.seed-nodes = [",
                (current, seed) => current + @"""" + seed + @""", ");
            seedConfigString += "]";

            var finalConfig = ConfigurationFactory.ParseString(seedConfigString)
                .WithFallback(clusterConfig);
            return ActorSystem.Create(systemName, finalConfig);
        }

        public void StartSeedNodes()
        {
            _actorSystem = CreateActorSystem();
            var pbm = PetabridgeCmd.Get(_actorSystem);
            pbm.RegisterCommandPalette(ClusterCommands.Instance);
            pbm.Start();
        }

        public async Task StopAsync()
        {
            await CoordinatedShutdown.Get(_actorSystem).Run();
        }
    }
}