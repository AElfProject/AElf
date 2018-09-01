using System;
using System.Collections.Generic;
using System.Threading;
using AElf.Management.Helper;
using AElf.Management.Models;
using k8s;
using k8s.Models;

namespace AElf.Management.Commands
{
    public class K8SAddRedisCommand : IDeployCommand
    {
        private const string ConfigName = "config-redis";
        private const string ServiceName = "service-redis";
        private const string StatefulSetName = "set-redis";
        private const int Replicas = 1;

        public void Action(string chainId, DeployArg arg)
        {
            AddConfig(chainId,arg);
            AddService(chainId,arg);
            var addSetResult = AddStatefulSet(chainId, arg);
            if (!addSetResult)
            {
                //throw new Exception("failed to deploy manager");
            }
        }

        private void AddConfig(string chainId, DeployArg arg)
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
                    {
                        ConfigName,
                        string.Concat("port ", arg.DBArg.Port, Environment.NewLine, "bind 0.0.0.0", Environment.NewLine, "appendonly no", Environment.NewLine)
                    }
                }
            };

            K8SRequestHelper.GetClient().CreateNamespacedConfigMap(body, chainId);
        }

        private void AddService(string chainId, DeployArg arg)
        {
            var body = new V1Service
            {
                Metadata = new V1ObjectMeta
                {
                    Name = ServiceName,
                    Labels = new Dictionary<string, string>
                    {
                        {"name", ServiceName}
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
                        {"name", StatefulSetName}
                    },
                    ClusterIP = "None"
                }
            };

            K8SRequestHelper.GetClient().CreateNamespacedService(body, chainId);
        }

        private bool AddStatefulSet(string chainId, DeployArg arg)
        {
            var body = new V1StatefulSet
            {
                Metadata = new V1ObjectMeta
                {
                    Name = StatefulSetName,
                    Labels = new Dictionary<string, string> {{"name", StatefulSetName}}
                },
                Spec = new V1StatefulSetSpec
                {
                    Selector = new V1LabelSelector
                    {
                        MatchExpressions = new List<V1LabelSelectorRequirement>
                        {
                            new V1LabelSelectorRequirement("name", "In", new List<string> {StatefulSetName})
                        }
                    },
                    ServiceName = ServiceName,
                    Replicas = Replicas,
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = new Dictionary<string, string> {{"name", StatefulSetName}}
                        },
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = StatefulSetName,
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
                                        Name = ConfigName,
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

            var result = K8SRequestHelper.GetClient().CreateNamespacedStatefulSet(body, chainId);

            var set = K8SRequestHelper.GetClient().ReadNamespacedStatefulSet(result.Metadata.Name, chainId);
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
                    DeletePod(chainId, arg);
                    retryDeleteCount++;
                    retryGetCount = 0;
                }
                
                if (retryDeleteCount > GlobalSetting.DeployRetryTime)
                {
                    return false;
                }

                retryGetCount++;
                Thread.Sleep(3000);
                set = K8SRequestHelper.GetClient().ReadNamespacedStatefulSet(result.Metadata.Name, chainId);
            }

            return true;
        }
        
        private void DeletePod(string chainId, DeployArg arg)
        {
            K8SRequestHelper.GetClient().DeleteCollectionNamespacedPod(chainId, labelSelector: "name=" + StatefulSetName);
        }
    }
}