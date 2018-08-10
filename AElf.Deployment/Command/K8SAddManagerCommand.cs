using System.Collections.Generic;
using AElf.Deployment.Helper;
using AElf.Deployment.Models;
using k8s.Models;

namespace AElf.Deployment.Command
{
    public class K8SAddManagerCommand:IDeployCommand
    {
        private const string ServiceName = "service-manager";
        private const string StatefulSetName = "set-manager";
        private const int Port = 4053;
        
        public void Action(string chainId, DeployArg arg)
        {
            DeployService(chainId,arg);
            DeployStatefulSet(chainId, arg);
        }

        private void DeployService(string chainId, DeployArg arg)
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
                        new V1ServicePort(Port)
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

        private void DeployStatefulSet(string chainId, DeployArg arg)
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
                                    Image = "aelf/node:manager",
                                    Ports = new List<V1ContainerPort> {new V1ContainerPort(Port)},
                                    Env = new List<V1EnvVar>
                                    {
                                        new V1EnvVar
                                        {
                                            Name = "POD_NAME",
                                            ValueFrom = new V1EnvVarSource {FieldRef = new V1ObjectFieldSelector("metadata.name")}
                                        }
                                    },
                                    Args = new List<string> {"--actor.host", "$(POD_NAME)." + ServiceName, "--actor.port", Port.ToString()},
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
                                    ConfigMap = new V1ConfigMapVolumeSource {Name = "config-common"}
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