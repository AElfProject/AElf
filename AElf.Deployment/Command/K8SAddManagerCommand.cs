using System.Collections.Generic;
using AElf.Deployment.Helper;
using AElf.Deployment.Models;
using k8s.Models;

namespace AElf.Deployment.Command
{
    public class K8SAddManagerCommand:IDeployCommand
    {
        public void Action(string chainId, DeployArg arg)
        {
            throw new System.NotImplementedException();
        }

        private void DeployService(DeployArg arg)
        {
            
        }

        private void DeployStatefulSet(string chainId, DeployArg arg)
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

            K8SRequestHelper.CreateNamespacedStatefulSet1(body, chainId);
        }
    }
}