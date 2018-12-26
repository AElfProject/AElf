using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Management.Helper;
using AElf.Management.Models;
using k8s;
using k8s.Models;

namespace AElf.Management.Commands
{
    public class K8SAddRedisCommand : IDeployCommand
    {
        private const int Replicas = 1;

        public async Task Action(DeployArg arg)
        {
            await AddConfig(arg);
            await AddService(arg);
            var addSetResult = await AddStatefulSet(arg);
            if (!addSetResult)
            {
                throw new Exception("failed to deploy redis");
            }
        }

        private async Task AddConfig(DeployArg arg)
        {
            var body = new V1ConfigMap
            {
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Kind = V1ConfigMap.KubeKind,
                Metadata = new V1ObjectMeta
                {
                    Name = GlobalSetting.RedisConfigName,
                    NamespaceProperty = arg.SideChainId
                },
                Data = new Dictionary<string, string>
                {
                    {
                        GlobalSetting.RedisConfigName,
                        string.Concat("port ", arg.DBArg.Port, Environment.NewLine, "bind 0.0.0.0", Environment.NewLine, "appendonly no", Environment.NewLine)
                    }
                }
            };

            await K8SRequestHelper.GetClient().CreateNamespacedConfigMapAsync(body, arg.SideChainId);
        }

        private async Task AddService(DeployArg arg)
        {
            var body = new V1Service
            {
                Metadata = new V1ObjectMeta
                {
                    Name = GlobalSetting.RedisServiceName,
                    Labels = new Dictionary<string, string>
                    {
                        {"name", GlobalSetting.RedisServiceName}
                    }
                },
                Spec = new V1ServiceSpec
                {
                    Ports = new List<V1ServicePort>
                    {
                        new V1ServicePort(arg.DBArg.Port)
                    },
                    Selector = new Dictionary<string, string>
                    {
                        {"name", GlobalSetting.RedisName}
                    },
                    ClusterIP = "None"
                }
            };

            await K8SRequestHelper.GetClient().CreateNamespacedServiceAsync(body, arg.SideChainId);
        }

        private async Task<bool> AddStatefulSet(DeployArg arg)
        {
            var body = new V1StatefulSet
            {
                Metadata = new V1ObjectMeta
                {
                    Name = GlobalSetting.RedisName,
                    Labels = new Dictionary<string, string> {{"name", GlobalSetting.RedisName}}
                },
                Spec = new V1StatefulSetSpec
                {
                    Selector = new V1LabelSelector
                    {
                        MatchExpressions = new List<V1LabelSelectorRequirement>
                        {
                            new V1LabelSelectorRequirement("name", "In", new List<string> {GlobalSetting.RedisName})
                        }
                    },
                    ServiceName = GlobalSetting.RedisServiceName,
                    Replicas = Replicas,
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = new Dictionary<string, string> {{"name", GlobalSetting.RedisName}}
                        },
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = GlobalSetting.RedisName,
                                    Image = "redis",
                                    Ports = new List<V1ContainerPort> {new V1ContainerPort(arg.DBArg.Port)},
                                    Command = new List<string> {"redis-server"},
                                    Args = new List<string> {"/redis/redis.conf"},
                                    VolumeMounts = new List<V1VolumeMount>
                                    {
                                        new V1VolumeMount("/redisdata", "data"),
                                        new V1VolumeMount("/redis", "config")
                                    }
                                }
                            },
                            Volumes = new List<V1Volume>
                            {
                                new V1Volume
                                {
                                    Name = "data",
                                    EmptyDir = new V1EmptyDirVolumeSource()
                                },
                                new V1Volume
                                {
                                    Name = "config",
                                    ConfigMap = new V1ConfigMapVolumeSource
                                    {
                                        Name = GlobalSetting.RedisConfigName,
                                        Items = new List<V1KeyToPath>
                                        {
                                            new V1KeyToPath
                                            {
                                                Key = "config-redis",
                                                Path = "redis.conf"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var result = await K8SRequestHelper.GetClient().CreateNamespacedStatefulSetAsync(body, arg.SideChainId);

            var set = await K8SRequestHelper.GetClient().ReadNamespacedStatefulSetAsync(result.Metadata.Name, arg.SideChainId);
            var retryGetCount = 0;
            var retryDeleteCount = 0;
            while (true)
            {
                if (set.Status.ReadyReplicas.HasValue && set.Status.ReadyReplicas.Value == Replicas)
                {
                    break;
                }

                if (retryGetCount > GlobalSetting.DeployRetryTime)
                {
                    await DeletePod(arg.SideChainId, arg);
                    retryDeleteCount++;
                    retryGetCount = 0;
                }

                if (retryDeleteCount > GlobalSetting.DeployRetryTime)
                {
                    return false;
                }

                retryGetCount++;
                Thread.Sleep(3000);
                set = await K8SRequestHelper.GetClient().ReadNamespacedStatefulSetAsync(result.Metadata.Name, arg.SideChainId);
            }

            return true;
        }

        private async Task DeletePod(string chainId, DeployArg arg)
        {
            await K8SRequestHelper.GetClient().DeleteCollectionNamespacedPodAsync(chainId, labelSelector: "name=" + GlobalSetting.RedisName);
        }
    }
}