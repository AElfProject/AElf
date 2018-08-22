using System.Collections.Generic;
using System.Linq;
using AElf.Configuration;
using AElf.Configuration.Config.Network;
using AElf.Management.Helper;
using AElf.Management.Models;
using k8s;
using k8s.Models;

namespace AElf.Management.Commands
{
    public class K8SAddConfigCommand : IDeployCommand
    {
        private const string ConfigName = "config-common";

        public void Action(string chainId, DeployArg arg)
        {
            var body = new V1ConfigMap
            {
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Kind = V1ConfigMap.KubeKind,
                Metadata = new V1ObjectMeta
                {
                    Name = ConfigName,
                    NamespaceProperty = chainId
                },
                Data = new Dictionary<string, string>
                {
                    {"actor.json", GetActorConfigJson(chainId, arg)}, 
                    {"database.json", GetDatabaseConfigJson(chainId, arg)}, 
                    {"miners.json", GetMinersConfigJson(chainId, arg)}, 
                    {"parallel.json", GetParallelConfigJson(chainId, arg)}, 
                    {"network.json", GetNetworkConfigJson(chainId, arg)}
                }
            };

            K8SRequestHelper.GetClient().CreateNamespacedConfigMap(body, chainId);
        }

        private string GetActorConfigJson(string chainId, DeployArg arg)
        {
            var config = new ActorConfig
            {
                IsCluster = arg.ManagerArg.IsCluster,
                HostName = "127.0.0.1",
                Port = 0,
                ActorCount = arg.WorkArg.ActorCount,
                Benchmark = false,
                ConcurrencyLevel = arg.WorkArg.ConcurrencyLevel,
                Seeds = new List<SeedNode> {new SeedNode {HostName = "set-manager-0.service-manager", Port = 4053}},
                SingleHoconFile = "single.hocon",
                MasterHoconFile = "master.hocon",
                WorkerHoconFile = "worker.hocon",
                ManagerHoconFile = "manager.hocon"
            };

            var result = JsonSerializer.Instance.Serialize(config);

            return result;
        }

        private string GetDatabaseConfigJson(string chainId, DeployArg arg)
        {
            var config = new DatabaseConfig
            {
                Type = DatabaseType.Redis,
                Host = "set-redis-0.service-redis",
                Port = 7001
            };

            var result = JsonSerializer.Instance.Serialize(config);

            return result;
        }

        private string GetMinersConfigJson(string chainId, DeployArg arg)
        {
            var config = new MinersConfig
            {
                Producers = new Dictionary<string, Dictionary<string, string>>
                {
                    {"1", new Dictionary<string, string> {{"address", "0x04b8b111fdbc2f5409a006339fa1758e1ed1"}}},
                    {"2", new Dictionary<string, string> {{"address", "0x0429c477d551aa91abc193d7088f69082000"}}},
                    {"3", new Dictionary<string, string> {{"address", "0x04bce3e67ec4fbd0fad2822e6e5ed097812c"}}}
                }
            };

            var result = JsonSerializer.Instance.Serialize(config);

            return result;
        }

        private string GetParallelConfigJson(string chainId, DeployArg arg)
        {
            var config = new ParallelConfig
            {
                IsParallelEnable = false
            };

            var result = JsonSerializer.Instance.Serialize(config);

            return result;
        }

        private string GetNetworkConfigJson(string chainId, DeployArg arg)
        {
            var config = new NetworkConfig();
            config.Bootnodes=new List<string>();
            config.Peers = new List<string>();

            if (arg.LauncherArg.Bootnodes != null && arg.LauncherArg.Bootnodes.Any())
            {
                config.Bootnodes = arg.LauncherArg.Bootnodes;
            }

            var result = JsonSerializer.Instance.Serialize(config);

            return result;
        }
        
    }
}