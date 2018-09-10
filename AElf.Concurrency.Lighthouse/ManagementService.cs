using System.Linq;
using System.Threading.Tasks;
using AElf.Configuration;
using Akka.Actor;
using Akka.Configuration;

namespace AElf.Concurrency.Lighthouse
{
    public class ManagementService
    {
        public static ActorSystem _actorSystem;
        public Task TerminationHandle => _actorSystem.WhenTerminated;

        private static ActorSystem CreateActorSystem()
        {
            var clusterConfig = ConfigurationFactory.ParseString(ActorConfig.Instance.LighthouseHoconConfig);
            var systemName = clusterConfig.GetConfig("manager").GetString("system-name");
//            var ipAddress = clusterConfig.GetConfig("akka.remote").GetString("dot-netty.tcp.hostname");
//            var port = clusterConfig.GetConfig("akka.remote").GetString("dot-netty.tcp.port");
            var ipAddress = ActorConfig.Instance.HostName;
            var port = ActorConfig.Instance.Port;
            var selfAddress = $"akka.tcp://{systemName}@{ipAddress}:{port}";

            var seeds = clusterConfig.GetStringList("akka.cluster.seed-nodes");
            if (!seeds.Contains(selfAddress))
                seeds.Add(selfAddress);

            var seedConfigString = seeds.Aggregate("akka.cluster.seed-nodes = [",
                (current, seed) => current + @"""" + seed + @""", ");
            seedConfigString += "]";
            
            var finalConfig = ConfigurationFactory.ParseString(seedConfigString)
                .WithFallback(ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.hostname=" + ActorConfig.Instance.HostName))
                .WithFallback(ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.port=" + ActorConfig.Instance.Port))
                .WithFallback(clusterConfig);
            return ActorSystem.Create(systemName, finalConfig);
        }

        public void StartSeedNodes()
        {
            _actorSystem = CreateActorSystem();
        }

        public async Task StopAsync()
        {
            await CoordinatedShutdown.Get(_actorSystem).Run();
        }
    }
}