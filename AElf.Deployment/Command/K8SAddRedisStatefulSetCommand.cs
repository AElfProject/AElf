using System.Collections.Generic;
using AElf.Deployment.Helper;
using AElf.Deployment.Models;
using k8s.Models;

namespace AElf.Deployment.Command
{
    public class K8SAddRedisStatefulSetCommand : IDeployCommand
    {
        public void Action(DeployArgument arg)
        {
            var body = new V1beta1StatefulSet
            {
                Metadata = new V1ObjectMeta
                {
                    Name = "set-redis",
                    Labels = new Dictionary<string, string> {{"name", "set-redis"}}
                },
                Spec = new V1beta1StatefulSetSpec
                {
                    Selector = new V1LabelSelector
                    {
                        MatchExpressions = new List<V1LabelSelectorRequirement>
                        {
                            new V1LabelSelectorRequirement("name", "In", new List<string> {"set-redis"})
                        }
                    },
                    ServiceName = "service-redis",
                    Replicas = 1,
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = new Dictionary<string, string> {{"name", "set-redis"}}
                        },
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = "set-redis",
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
                                    Name = "data",
                                    EmptyDir = new V1EmptyDirVolumeSource()
                                },
                                new V1Volume
                                {
                                    Name = "config",
                                    ConfigMap = new V1ConfigMapVolumeSource
                                    {
                                        Name = "config-redis",
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

            K8SRequestHelper.CreateNamespacedStatefulSet1(body, "default");
        }
    }
}