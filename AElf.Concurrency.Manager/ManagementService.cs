using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Configuration;
using Akka.Actor;
using Akka.Configuration;
using Akka.Util.Internal;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Host;

namespace AElf.Concurrency.Manager
{
    public class ManagementService
    {
        public static ActorSystem _actorSystem;
        public Task TerminationHandle => _actorSystem.WhenTerminated;

        private static ActorSystem CreateActorSystem()
        {
            var clusterConfig = ConfigurationFactory.ParseString(ActorConfig.Instance.ManagerHoconConfig);
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
            
            var receptionistAddress = string.Format("akka.tcp://{0}@{1}:{2}/system/receptionist", systemName, ipAddress, port);

            var finalConfig = ConfigurationFactory.ParseString(seedConfigString)
                .WithFallback(ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.hostname=" + ActorConfig.Instance.HostName))
                .WithFallback(ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.port=" + ActorConfig.Instance.Port))
                .WithFallback(ConfigurationFactory.ParseString(@"akka.cluster.client.initial-contacts = [""" + receptionistAddress + @"""]"))
                .WithFallback(clusterConfig);
            return ActorSystem.Create(systemName, finalConfig);
        }

        public void StartSeedNodes()
        {
            _actorSystem = CreateActorSystem();
//            var pbm = PetabridgeCmd.Get(_actorSystem);
//            pbm.RegisterCommandPalette(ClusterCommands.Instance);
//            pbm.Start();
            
            var listener = _actorSystem.ActorOf(Props.Create(typeof(ClusterListener)), "clusterListener");
            
//            var _clusterManagerActor  = _actorSystem.ActorOf(Props.Create(() => new ManagerActor2()));
//            //_clusterManagerActor.Tell(new SubscribeToManager());
//            
//            _clusterManagerActor.Tell(new StartSchedule(2));
//            
//            _actorSystem

//            var i = 0;
//            while (true)
//            {
//                Thread.Sleep(3000);
//                
//                Akka.Cluster.Cluster.Get(_actorSystem).SendCurrentClusterState(listener);
//                i++;
//
//                if (i == 5)
//                {
//                    Akka.Cluster.Cluster.Get(_actorSystem).Down(new Address("akka.tcp", "AElfSystem", "127.0.0.1", 2551));
//
//                }
//
//            }
        }

        public async Task StopAsync()
        {
            await CoordinatedShutdown.Get(_actorSystem).Run();
        }
    }
}