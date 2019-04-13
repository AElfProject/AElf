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
    public class K8SAddWorkerCommand : IDeployCommand
    {
        private const int Port = 32551;

        public async Task Action(DeployArg arg)
        {
            if (arg.LighthouseArg.IsCluster)
            {
                var addDeployResult = await AddDeployment(arg);
                if (!addDeployResult)
                {
                    throw new Exception("failed to deploy worker");
                }
            }
        }

        private async Task<bool> AddDeployment(DeployArg arg)
        {
            var body = new V1Deployment
            {
                //ApiVersion = "extensions/v1beta1",
                Kind = "Deployment",
                Metadata = new V1ObjectMeta
                {
                    Name = GlobalSetting.WorkerName,
                    Labels = new Dictionary<string, string> {{"name", GlobalSetting.WorkerName}}
                },

                Spec = new V1DeploymentSpec
                {
                    Selector = new V1LabelSelector {MatchLabels = new Dictionary<string, string> {{"name", GlobalSetting.WorkerName}}},
                    Replicas = arg.WorkArg.WorkerCount,
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta {Labels = new Dictionary<string, string> {{"name", GlobalSetting.WorkerName}}},
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = GlobalSetting.WorkerName,
                                    Image = "aelf/node:test",
                                    Ports = new List<V1ContainerPort>
                                    {
                                        new V1ContainerPort(Port)
                                    },
                                    Env = new List<V1EnvVar>
                                    {
                                        new V1EnvVar
                                        {
                                            Name = "POD_IP",
                                            ValueFrom = new V1EnvVarSource {FieldRef = new V1ObjectFieldSelector {FieldPath = "status.podIP"}}
                                        }
                                    },
                                    Command = new List<string> {"dotnet", "AElf.Concurrency.Worker.dll"},
                                    Args = new List<string> {"--actor.host", "$(POD_IP)", "--actor.port", Port.ToString()},
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

            var result = await K8SRequestHelper.GetClient().CreateNamespacedDeploymentAsync(body, arg.SideChainId);

            var deploy = await K8SRequestHelper.GetClient().ReadNamespacedDeploymentAsync(result.Metadata.Name, arg.SideChainId);
            var retryGetCount = 0;
            var retryDeleteCount = 0;
            while (true)
            {
                if (deploy.Status.ReadyReplicas.HasValue && deploy.Status.ReadyReplicas.Value == arg.WorkArg.WorkerCount)
                {
                    break;
                }

                if (retryGetCount > GlobalSetting.DeployRetryTime)
                {
                    await DeletePod(arg);
                    retryDeleteCount++;
                    retryGetCount = 0;
                }

                if (retryDeleteCount > GlobalSetting.DeployRetryTime)
                {
                    return false;
                }

                retryGetCount++;
                Thread.Sleep(3000);
                deploy = await K8SRequestHelper.GetClient().ReadNamespacedDeploymentAsync(result.Metadata.Name, arg.SideChainId);
            }

            return true;
        }

        private async Task DeletePod(DeployArg arg)
        {
            await K8SRequestHelper.GetClient().DeleteCollectionNamespacedPodAsync(arg.SideChainId, labelSelector: "name=" + GlobalSetting.WorkerName);
        }
    }
}