using System.Collections.Generic;
using AElf.Configuration;
using AElf.Management.Helper;
using AElf.Management.Models;
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
                    {"miners.json", GetMinersConfigJson(chainId, arg)}
                }
            };

            K8SRequestHelper.CreateNamespacedConfigMap(body, chainId);
        }

        private string GetActorConfigJson(string chainId, DeployArg arg)
        {
            var config = new ActorConfig
            {
                IsCluster = true,
                HostName = "127.0.0.1",
                Port = 0,
                WorkerCount = arg.WorkArg.ActorCount,
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
                    {"1", new Dictionary<string, string> {{"address", arg.MainChainAccount}}}
                }
            };

            var result = JsonSerializer.Instance.Serialize(config);

            return result;
        }
    }
}