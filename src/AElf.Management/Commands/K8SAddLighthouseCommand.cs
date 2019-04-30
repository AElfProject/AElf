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
    public class K8SAddLighthouseCommand : IDeployCommand
    {
        private const int Port = 4053;
        private const int Replicas = 1;

        public async Task Action(DeployArg arg)
        {
            if (arg.LighthouseArg.IsCluster)
            {
                await AddService(arg);
                var addSetResult = await AddStatefulSet(arg);
                if (!addSetResult)
                {
                    throw new Exception("failed to deploy lighthouse");
                }
            }
        }

        private async Task AddService(DeployArg arg)
        {
            var body = new V1Service
            {
                Metadata = new V1ObjectMeta
                {
                    Name = GlobalSetting.LighthouseServiceName,
                    Labels = new Dictionary<string, string>
                    {
                        {"name", GlobalSetting.LighthouseServiceName}
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
                        {"name", GlobalSetting.LighthouseName}
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
                    Name = GlobalSetting.LighthouseName,
                    Labels = new Dictionary<string, string> {{"name", GlobalSetting.LighthouseName}}
                },
                Spec = new V1StatefulSetSpec
                {
                    Selector = new V1LabelSelector
                    {
                        MatchExpressions = new List<V1LabelSelectorRequirement>
                        {
                            new V1LabelSelectorRequirement("name", "In", new List<string> {GlobalSetting.LighthouseName})
                        }
                    },
                    ServiceName = GlobalSetting.LighthouseServiceName,
                    Replicas = Replicas,
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = new Dictionary<string, string> {{"name", GlobalSetting.LighthouseName}}
                        },
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = GlobalSetting.LighthouseName,
                                    Image = "aelf/node:test",
                                    ImagePullPolicy = "Always",
                                    Ports = new List<V1ContainerPort>
                                    {
                                        new V1ContainerPort(Port)
                                    },
                                    Env = new List<V1EnvVar>
                                    {
                                        new V1EnvVar
                                        {
                                            Name = "POD_NAME",
                                            ValueFrom = new V1EnvVarSource {FieldRef = new V1ObjectFieldSelector("metadata.name")}
                                        }
                                    },
                                    Command = new List<string> {"dotnet", "AElf.Concurrency.Lighthouse.dll"},
                                    Args = new List<string> {"--actor.host", "$(POD_NAME)." + GlobalSetting.LighthouseServiceName, "--actor.port", Port.ToString()},
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
                set = await K8SRequestHelper.GetClient().ReadNamespacedStatefulSetAsync(result.Metadata.Name, arg.SideChainId);
            }

            return true;
        }

        private async Task DeletePod(DeployArg arg)
        {
            await K8SRequestHelper.GetClient().DeleteCollectionNamespacedPodAsync(arg.SideChainId, labelSelector: "name=" + GlobalSetting.LighthouseName);
        }
    }
}