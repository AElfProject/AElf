using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Management.Helper;
using AElf.Management.Models;
using k8s;
using k8s.Models;

namespace AElf.Management.Commands
{
    public class K8SAddMonitorCommand: IDeployCommand
    {
        private const int MonitorPort = 9099;
        private const int ActorPort = 31550;
        private const int Replicas = 1;

        public void Action(string chainId, DeployArg arg)
        {
            AddService(chainId, arg);

            var addDeployResult = AddDeployment(chainId, arg);

            if (!addDeployResult)
            {
                throw new Exception("failed to deploy monitor");
            }
        }

        private void AddService(string chainId, DeployArg arg)
        {
            var body = new V1Service
            {
                Metadata = new V1ObjectMeta
                {
                    Name = GlobalSetting.MonitorServiceName,
                    Labels = new Dictionary<string, string>
                    {
                        {"name", GlobalSetting.MonitorServiceName}
                    }
                },
                Spec = new V1ServiceSpec
                {
                    Type = "LoadBalancer",
                    Ports = new List<V1ServicePort>
                    {
                        new V1ServicePort(MonitorPort, "monitor-port", null, "TCP", MonitorPort)
                    },
                    Selector = new Dictionary<string, string>
                    {
                        {"name", GlobalSetting.MonitorName}
                    }
                }
            };

            K8SRequestHelper.GetClient().CreateNamespacedService(body, chainId);
        }

        private bool AddDeployment(string chainId, DeployArg arg)
        {
            var body = new V1Deployment
            {
                //ApiVersion = "extensions/v1beta1",
                Kind = "Deployment",
                Metadata = new V1ObjectMeta
                {
                    Name = GlobalSetting.MonitorName,
                    Labels = new Dictionary<string, string> {{"name", GlobalSetting.MonitorName}}
                },

                Spec = new V1DeploymentSpec
                {
                    Selector = new V1LabelSelector {MatchLabels = new Dictionary<string, string> {{"name", GlobalSetting.MonitorName}}},
                    Replicas = Replicas,
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta {Labels = new Dictionary<string, string> {{"name", GlobalSetting.MonitorName}}},
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = GlobalSetting.MonitorName,
                                    Image = "aelf/node:test",
                                    Ports = new List<V1ContainerPort>
                                    {
                                        new V1ContainerPort(MonitorPort),
                                        new V1ContainerPort(ActorPort)
                                    },
                                    ImagePullPolicy = "Always",
                                    Env = new List<V1EnvVar>
                                    {
                                        new V1EnvVar
                                        {
                                            Name = "POD_IP",
                                            ValueFrom = new V1EnvVarSource {FieldRef = new V1ObjectFieldSelector {FieldPath = "status.podIP"}}
                                        }
                                    },
                                    Command = new List<string> {"dotnet", "AElf.Monitor.dll"},
                                    Args = new List<string>
                                    {
                                        "--actor.host",
                                        "$(POD_IP)",
                                        "--actor.port",
                                        ActorPort.ToString()
                                    },
                                    VolumeMounts = new List<V1VolumeMount>
                                    {
                                        new V1VolumeMount
                                        {
                                            MountPath = "/app/aelf/config",
                                            Name = "config"
                                        }
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

            var result = K8SRequestHelper.GetClient().CreateNamespacedDeployment(body, chainId);
            
            var deploy = K8SRequestHelper.GetClient().ReadNamespacedDeployment(result.Metadata.Name, chainId);
            var retryGetCount = 0;
            var retryDeleteCount = 0;
            while (true)
            {
                if (deploy.Status.ReadyReplicas.HasValue && deploy.Status.ReadyReplicas.Value == Replicas)
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
                deploy = K8SRequestHelper.GetClient().ReadNamespacedDeployment(result.Metadata.Name, chainId);
            }

            return true;
        }

        private void DeletePod(string chainId, DeployArg arg)
        {
            K8SRequestHelper.GetClient().DeleteCollectionNamespacedPod(chainId, labelSelector: "name=" + GlobalSetting.MonitorName);
        }
    }
}