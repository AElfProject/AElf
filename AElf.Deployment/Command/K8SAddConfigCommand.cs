using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Input;
using AElf.Configuration;
using AElf.Deployment.Helper;
using AElf.Deployment.Models;
using k8s.Models;

namespace AElf.Deployment.Command
{
    public class K8SAddConfigCommand : IDeployCommand
    {
        private const string ConfigName = "config-common";

        public void Action(string chainId, DeployArg arg)
        {
            var body = new V1ConfigMap();
            body.ApiVersion = V1ConfigMap.KubeApiVersion;
            body.Kind = V1ConfigMap.KubeKind;
            body.Metadata = new V1ObjectMeta();
            body.Metadata.Name = ConfigName;
            body.Metadata.NamespaceProperty = chainId;
            body.Data = new Dictionary<string, string>();
            body.Data.Add("actor.json", GetActorConfigJson());
            body.Data.Add("database.json", GetDatabaseConfigJson());

            K8SRequestHelper.CreateNamespacedConfigMap(body, chainId);
        }

        private string GetActorConfigJson()
        {
            var actorConfig = new ActorConfig();
            actorConfig.IsCluster = true;
            actorConfig.HostName = "127.0.0.1";
            actorConfig.Port = 0;
            actorConfig.WorkerCount = 8;
            actorConfig.Benchmark = false;
            actorConfig.ConcurrencyLevel = 16;
            actorConfig.Seeds = new List<SeedNode>();
            actorConfig.Seeds.Add(new SeedNode {HostName = "set-manager-0.service-manager", Port = 4053});
            actorConfig.SingleHoconFile = "single.hocon";
            actorConfig.MasterHoconFile = "master.hocon";
            actorConfig.WorkerHoconFile = "worker.hocon";
            actorConfig.ManagerHoconFile = "manager.hocon";

            var result = JsonSerializer.Instance.Serialize(actorConfig);

            return result;
        }

        private string GetDatabaseConfigJson()
        {
            var databaseConfig = new DatabaseConfig();
            databaseConfig.Type = DatabaseType.Redis;
            databaseConfig.Host = "set-redis-0.service-redis";
            databaseConfig.Port = 7001;

            var result = JsonSerializer.Instance.Serialize(databaseConfig);

            return result;
        }
    }
}