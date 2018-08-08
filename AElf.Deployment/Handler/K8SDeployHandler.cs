using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AElf.Deployment.Command;
using AElf.Deployment.Helper;
using AElf.Deployment.Models;
using k8s.Models;
using ICommand = System.Windows.Input.ICommand;

namespace AElf.Deployment.Handler
{
    public class K8SDeployHandler : IDeployHandler
    {
        private static readonly IDeployHandler _instance = new K8SDeployHandler();

        public static IDeployHandler Instance
        {
            get { return _instance; }
        }
        
        private readonly List<IDeployCommand> _commands =new List<IDeployCommand>();

        private K8SDeployHandler()
        {
            _commands.Add(new K8SAddReidsConfigCommand());
            _commands.Add(new K8SAddRedisServiceCommand());
            _commands.Add(new K8SAddRedisStatefulSetCommand());
        }

        public void Deploy(DeployArgument arg)
        {
            foreach (var cmd in _commands)
            {
                cmd.Action(arg);
            }
        }

        
//        private void DeployManagerStatefulSet()
//        {
//            var body = new V1beta1StatefulSet
//            {
//                Metadata = new V1ObjectMeta
//                {
//                    Name = "pod-redis",
//                    Labels = new Dictionary<string, string> {{"name", "pod-redis"}}
//                },
//                Spec = new V1beta1StatefulSetSpec
//                {
//                    Selector = new V1LabelSelector
//                    {
//                        MatchExpressions = new List<V1LabelSelectorRequirement>
//                        {
//                            new V1LabelSelectorRequirement("name", "In", new List<string> {"pod-redis"})
//                        }
//                    },
//                    ServiceName = "service-redis",
//                    Replicas = 1,
//                    Template = new V1PodTemplateSpec
//                    {
//                        Metadata = new V1ObjectMeta
//                        {
//                            Labels = new Dictionary<string, string> {{"name", "pod-redis"}}
//                        },
//                        Spec = new V1PodSpec
//                        {
//                            Containers = new List<V1Container>
//                            {
//                                new V1Container
//                                {
//                                    Name = "pod-redis",
//                                    Image = "redis",
//                                    Ports = new List<V1ContainerPort> {new V1ContainerPort(7001)},
//                                    Env = new List<V1EnvVar>
//                                    {
//                                        new V1EnvVar
//                                        {
//                                            Name = "POD_NAME",
//                                            ValueFrom = new V1EnvVarSource {FieldRef = new V1ObjectFieldSelector("metadata.name")}
//                                        }
//                                    },
//                                    Args = new List<string> {"--actor.host", "$(POD_NAME).manager-service", "--actor.port", "4053"},
//                                    VolumeMounts = new List<V1VolumeMount>
//                                    {
//                                        new V1VolumeMount("/app/aelf/config", "config")
//                                    }
//                                }
//                            },
//                            Volumes = new List<V1Volume>
//                            {
//                                new V1Volume
//                                {
//                                    Name = "config",
//                                    ConfigMap = new V1ConfigMapVolumeSource {Name = "aelf-config"}
//                                }
//                            }
//                        }
//                    }
//                }
//            };
//
//            K8SRequestHelper.CreateNamespacedStatefulSet1(body, "default");
//        }
    }
}