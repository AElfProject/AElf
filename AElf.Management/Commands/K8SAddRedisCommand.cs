using System;
using System.Collections.Generic;
using AElf.Management.Helper;
using AElf.Management.Models;
using k8s.Models;

namespace AElf.Management.Commands
{
    public class K8SAddRedisCommand : IDeployCommand
    {
        private const string ConfigName = "config-redis";
        private const string ServiceName = "service-redis";
        private const string StatefulSetName = "set-redis";

        public void Action(string chainId, DeployArg arg)
        {
            AddConfig(chainId,arg);
            AddService(chainId,arg);
            AddStatefulSet(chainId,arg);
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

            K8SRequestHelper.CreateNamespacedConfigMap(body, chainId);
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

            K8SRequestHelper.CreateNamespacedService(body, chainId);
        }

        private void AddStatefulSet(string chainId, DeployArg arg)
        {
            var body = new V1beta1StatefulSet
            {
                Metadata = new V1ObjectMeta
                {
                    Name = StatefulSetName,
                    Labels = new Dictionary<string, string> {{"name", StatefulSetName}}
                },
                Spec = new V1beta1StatefulSetSpec
                {
                    Selector = new V1LabelSelector
                    {
                        MatchExpressions = new List<V1LabelSelectorRequirement>
                        {
                            new V1LabelSelectorRequirement("name", "In", new List<string> {StatefulSetName})
                        }
                    },
                    ServiceName = ServiceName,
                    Replicas = 1,
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
                                    Resources = new V1ResourceRequirements
                                    {
                                        Limits = new Dictionary<string, ResourceQuantity>()
                                        {
                                            {"cpu", new ResourceQuantity("0.1")}
                                        }
                                    },
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

            K8SRequestHelper.CreateNamespacedStatefulSet1(body, chainId);
        }
    }
}