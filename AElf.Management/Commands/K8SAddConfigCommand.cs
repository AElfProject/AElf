using System.Collections.Generic;
using System.Linq;
using AElf.Common.Enums;
using AElf.Configuration;
using AElf.Configuration.Config.GRPC;
using AElf.Configuration.Config.Network;
using AElf.Management.Helper;
using AElf.Management.Models;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace AElf.Management.Commands
{
    public class K8SAddConfigCommand : IDeployCommand
    {
        public void Action(DeployArg arg)
        {
            var body = new V1ConfigMap
            {
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Kind = V1ConfigMap.KubeKind,
                Metadata = new V1ObjectMeta
                {
                    Name = GlobalSetting.CommonConfigName,
                    NamespaceProperty = arg.SideChainId
                },
                Data = new Dictionary<string, string>
                {
                    {"actor.json", GetActorConfigJson(arg)}, 
                    {"database.json", GetDatabaseConfigJson(arg)}, 
                    {"miners.json", GetMinersConfigJson(arg)}, 
                    {"parallel.json", GetParallelConfigJson(arg)}, 
                    {"network.json", GetNetworkConfigJson(arg)},
                    {"grpclocal.json",GetGrpcConfigJson(arg)},
                    {"grpcremote.json",GetGrpcRemoteConfigJson(arg)}
                }
            };

            K8SRequestHelper.GetClient().CreateNamespacedConfigMap(body, arg.SideChainId);

            if (!arg.IsDeployMainChain)
            {
                var config = K8SRequestHelper.GetClient().ReadNamespacedConfigMap(GlobalSetting.CommonConfigName, arg.MainChainId);

                var grpcRemoteConfig = JsonSerializer.Instance.Deserialize<GrpcRemoteConfig>(config.Data["grpcremote.json"]);
                grpcRemoteConfig.ChildChains.Add(arg.SideChainId, new Uri {Port = GlobalSetting.GrpcPort, Address = arg.LauncherArg.ClusterIp});

                var patch = new JsonPatchDocument<V1ConfigMap>();
                patch.Add(e => e.Data, new Dictionary<string, string> {{"grpcremote.json", JsonSerializer.Instance.Serialize(grpcRemoteConfig)}});

                K8SRequestHelper.GetClient().PatchNamespacedConfigMap(new V1Patch(patch), GlobalSetting.CommonConfigName, arg.MainChainId);
            }
        }

        private string GetActorConfigJson(DeployArg arg)
        {
            var config = new ActorConfig
            {
                IsCluster = arg.LighthouseArg.IsCluster,
                HostName = "127.0.0.1",
                Port = 0,
                ActorCount = arg.WorkArg.ActorCount,
                ConcurrencyLevel = arg.WorkArg.ConcurrencyLevel,
                Seeds = new List<SeedNode> {new SeedNode {HostName = "set-lighthouse-0.service-lighthouse", Port = 4053}},
                SingleHoconFile = "single.hocon",
                MasterHoconFile = "master.hocon",
                WorkerHoconFile = "worker.hocon",
                LighthouseHoconFile = "lighthouse.hocon",
                MonitorHoconFile = "monitor.hocon"
            };

            var result = JsonSerializer.Instance.Serialize(config);

            return result;
        }

        private string GetDatabaseConfigJson(DeployArg arg)
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

        private string GetMinersConfigJson(DeployArg arg)
        {
            var config = new MinersConfig();
            var i = 1;
            config.Producers=new Dictionary<string, Dictionary<string, string>>();
            foreach (var miner in arg.Miners)
            {
                config.Producers.Add(i.ToString(),new Dictionary<string, string>{{"address",miner}});
                i++;
            }

            var result = JsonSerializer.Instance.Serialize(config);

            return result;
        }

        private string GetParallelConfigJson(DeployArg arg)
        {
            var config = new ParallelConfig
            {
                IsParallelEnable = false
            };

            var result = JsonSerializer.Instance.Serialize(config);

            return result;
        }

        private string GetNetworkConfigJson(DeployArg arg)
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

        private string GetGrpcConfigJson(DeployArg arg)
        {
            var config = new GrpcLocalConfig
            {
                LocalServerIP = arg.LauncherArg.ClusterIp,
                LocalServerPort = GlobalSetting.GrpcPort,
                Client = true,
                WaitingIntervalInMillisecond = 10,
                Server = true
            };
            
            var result = JsonSerializer.Instance.Serialize(config);

            return result;
        }

        private string GetGrpcRemoteConfigJson(DeployArg arg)
        {
            var config = new GrpcRemoteConfig()
            {
                ParentChain = new Dictionary<string, Uri>(),
                ChildChains = new Dictionary<string, Uri>()
            };

            if (!arg.IsDeployMainChain)
            {
                var service = K8SRequestHelper.GetClient().ReadNamespacedService(GlobalSetting.LauncherServiceName, arg.MainChainId);
                config.ParentChain.Add(arg.MainChainId, new Uri {Port = GlobalSetting.GrpcPort, Address = service.Status.LoadBalancer.Ingress.FirstOrDefault().Ip});
            }

            var result = JsonSerializer.Instance.Serialize(config);

            return result;
        }
    }
}