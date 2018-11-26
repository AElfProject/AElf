using System.Linq;
using AElf.Common.Module;
using AElf.Configuration;
using Akka.Actor;
using Akka.Configuration;
using Autofac;

namespace AElf.Monitor
{
    public class AkkaModule:IAElfModule
    {
        public void Init(ContainerBuilder builder)
        {
        }

        public void Run(ILifetimeScope scope)
        {
            if (!ActorConfig.Instance.IsCluster)
            {
                return;
            }

            var clusterConfig = ConfigurationFactory.ParseString(ActorConfig.Instance.MonitorHoconConfig);
            var systemName = clusterConfig.GetConfig("manager").GetString("system-name");
            var ipAddress = ActorConfig.Instance.HostName;
            var port = ActorConfig.Instance.Port;
            var selfAddress = $"akka.tcp://{systemName}@{ipAddress}:{port}";

            var seeds = string.Join(",",
                ActorConfig.Instance.Seeds.Select(s => $@"""akka.tcp://{systemName}@{s.HostName}:{s.Port}"""));
            var seedsString = $"akka.cluster.seed-nodes = [{seeds}]";
            
            var finalConfig = ConfigurationFactory.ParseString(seedsString)
                .WithFallback(ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.hostname=" + ActorConfig.Instance.HostName))
                .WithFallback(ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.port=" + ActorConfig.Instance.Port))
                .WithFallback(clusterConfig);
            var actorSystem = ActorSystem.Create(systemName, finalConfig);
            
            actorSystem.ActorOf(Props.Create(typeof(AkkaClusterListener)), "clusterListener");
        }
    }
}