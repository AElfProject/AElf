using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AElf.Deployment.Helper;
using k8s.Models;

namespace AElf.Deployment.Handler
{
    public class K8SDeployHandler : IDeployHandler
    {
        private static readonly IDeployHandler _instance = new K8SDeployHandler();

        public static IDeployHandler Instance
        {
            get { return _instance; }
        }

        private K8SDeployHandler()
        {
        }

        public void Execute()
        {
            throw new System.NotImplementedException();
        }

        private void DeployDBConfig()
        {
            var body = new V1ConfigMap
            {
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Kind = V1ConfigMap.KubeKind,
                Metadata = new V1ObjectMeta
                {
                    Name = "config-redis",
                    NamespaceProperty = "default"
                },
                Data = new Dictionary<string, string>
                {
                    {
                        "config-redis",
                        string.Concat("port 7001", Environment.NewLine, "bind 0.0.0.0", Environment.NewLine, "appendonly no", Environment.NewLine)
                    }
                }
            };

            K8SRequestHelper.CreateNamespacedConfigMap(body, "default");
        }

        private void DeployDBService()
        {
            var body = new V1Service
            {
                Metadata = new V1ObjectMeta
                {
                    Name = "service-redis",
                    Labels = new Dictionary<string, string>
                    {
                        {"name", "service-redis"}
                    }
                },
                Spec = new V1ServiceSpec
                {
                    Ports = new List<V1ServicePort>
                    {
                        new V1ServicePort(7001)
                    },
                    Selector = new Dictionary<string, string>
                    {
                        {"name", "pod-redis"}
                    },
                    ClusterIP = "None"
                }
            };

            K8SRequestHelper.CreateNamespacedService(body, "default");
        }

        private void DeployDBStatefulSet()
        {
            var body = new V1beta1StatefulSet
            {
                Metadata = new V1ObjectMeta
                {
                    Name = "pod-redis",
                    Labels = new Dictionary<string, string> {{"name", "pod-redis"}}
                },
                Spec = new V1beta1StatefulSetSpec
                {
                    Selector = new V1LabelSelector
                    {
                        MatchExpressions = new List<V1LabelSelectorRequirement>
                        {
                            new V1LabelSelectorRequirement("name", "In", new List<string> {"pod-redis"})
                        }
                    },
                    ServiceName = "service-redis",
                    Replicas = 1,
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = new Dictionary<string, string> {{"name", "pod-redis"}}
                        },
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = "pod-redis",
                                    Image = "redis",
                                    Ports = new List<V1ContainerPort> {new V1ContainerPort(7001)},
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
                                    Name = "config",
                                    ConfigMap = new V1ConfigMapVolumeSource {Name = "aelf-config"}
                                }
                            }
                        }
                    }
                }
            };

            K8SRequestHelper.CreateNamespacedStatefulSet1(body, "default");
        }
        
        private void DeployManagerStatefulSet()
        {
            var body = new V1beta1StatefulSet
            {
                Metadata = new V1ObjectMeta
                {
                    Name = "pod-redis",
                    Labels = new Dictionary<string, string> {{"name", "pod-redis"}}
                },
                Spec = new V1beta1StatefulSetSpec
                {
                    Selector = new V1LabelSelector
                    {
                        MatchExpressions = new List<V1LabelSelectorRequirement>
                        {
                            new V1LabelSelectorRequirement("name", "In", new List<string> {"pod-redis"})
                        }
                    },
                    ServiceName = "service-redis",
                    Replicas = 1,
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = new Dictionary<string, string> {{"name", "pod-redis"}}
                        },
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = "pod-redis",
                                    Image = "redis",
                                    Ports = new List<V1ContainerPort> {new V1ContainerPort(7001)},
                                    Env = new List<V1EnvVar>
                                    {
                                        new V1EnvVar
                                        {
                                            Name = "POD_NAME",
                                            ValueFrom = new V1EnvVarSource {FieldRef = new V1ObjectFieldSelector("metadata.name")}
                                        }
                                    },
                                    Args = new List<string> {"--actor.host", "$(POD_NAME).manager-service", "--actor.port", "4053"},
                                    VolumeMounts = new List<V1VolumeMount>
                                    {
                                        new V1VolumeMount("/app/aelf/config", "config")
                                    }
                                }
                            },
                            Volumes = new List<V1Volume>
                            {
                                new V1Volume
                                {
                                    Name = "config",
                                    ConfigMap = new V1ConfigMapVolumeSource {Name = "aelf-config"}
                                }
                            }
                        }
                    }
                }
            };

            K8SRequestHelper.CreateNamespacedStatefulSet1(body, "default");
        }
    }
}