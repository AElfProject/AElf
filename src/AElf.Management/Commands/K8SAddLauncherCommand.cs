using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Management.Helper;
using AElf.Management.Models;
using k8s;
using k8s.Models;

namespace AElf.Management.Commands
{
    public class K8SAddLauncherCommand : IDeployCommand
    {
        private const int ActorPort = 32550;
        private const int Replicas = 1;

        public async Task Action(DeployArg arg)
        {
            var addDeployResult = await AddDeployment(arg);

            if (!addDeployResult)
            {
                throw new Exception("failed to deploy launcher");
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
                    Name = GlobalSetting.LauncherName,
                    Labels = new Dictionary<string, string> {{"name", GlobalSetting.LauncherName}}
                },

                Spec = new V1DeploymentSpec
                {
                    Selector = new V1LabelSelector {MatchLabels = new Dictionary<string, string> {{"name", GlobalSetting.LauncherName}}},
                    Replicas = Replicas,
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta {Labels = new Dictionary<string, string> {{"name", GlobalSetting.LauncherName}}},
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = GlobalSetting.LauncherName,
                                    Image = "aelf/node:test",
                                    Ports = new List<V1ContainerPort>
                                    {
                                        new V1ContainerPort(GlobalSetting.NodePort),
                                        new V1ContainerPort(GlobalSetting.RpcPort),
                                        new V1ContainerPort(ActorPort),
                                        new V1ContainerPort(GlobalSetting.GrpcPort)
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
                                    Command = new List<string> {"dotnet", "AElf.Launcher.dll"},
                                    Args = new List<string>
                                    {
                                        "--mine.enable",
                                        "true",
                                        "--rpc.host",
                                        "0.0.0.0",
                                        "--rpc.port",
                                        GlobalSetting.RpcPort.ToString(),
                                        "--node.account",
                                        arg.ChainAccount,
                                        "--node.port",
                                        GlobalSetting.NodePort.ToString(),
                                        "--actor.host",
                                        "$(POD_IP)",
                                        "--actor.port",
                                        ActorPort.ToString(),
                                        "--node.accountpassword",
                                        arg.AccountPassword,
                                        "--dpos.generator",
                                        arg.LauncherArg.IsConsensusInfoGenerator.ToString(),
                                        "--chain.id",
                                        arg.SideChainId,
                                        "--node.executor",
                                        arg.LighthouseArg.IsCluster ? "akka" : "simple"
                                    },
                                    VolumeMounts = new List<V1VolumeMount>
                                    {
                                        new V1VolumeMount
                                        {
                                            MountPath = "/app/aelf/config",
                                            Name = "config"
                                        },
                                        new V1VolumeMount
                                        {
                                            MountPath = "/app/aelf/keys",
                                            Name = "key"
                                        },
                                        new V1VolumeMount
                                        {
                                            MountPath = "/app/aelf/certs",
                                            Name = "cert"
                                        }
                                    }
                                }
                            },
                            Volumes = new List<V1Volume>
                            {
                                new V1Volume
                                {
                                    Name = "config",
                                    ConfigMap = new V1ConfigMapVolumeSource {Name = GlobalSetting.CommonConfigName}
                                },
                                new V1Volume
                                {
                                    Name = "key",
                                    ConfigMap = new V1ConfigMapVolumeSource
                                    {
                                        Name = GlobalSetting.KeysConfigName,
                                        Items = new List<V1KeyToPath>
                                        {
                                            new V1KeyToPath {Key = arg.ChainAccount + ".ak", Path = arg.ChainAccount + ".ak"}
                                        }
                                    }
                                },
                                new V1Volume
                                {
                                    Name = "cert",
                                    ConfigMap = new V1ConfigMapVolumeSource {Name = GlobalSetting.CertsConfigName}
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
                if (deploy.Status.ReadyReplicas.HasValue && deploy.Status.ReadyReplicas.Value == Replicas)
                {
                    break;
                }

                if (retryGetCount > GlobalSetting.DeployRetryTime)
                {
                    await K8SRequestHelper.GetClient().DeleteCollectionNamespacedPodAsync(arg.SideChainId, labelSelector: "name=" + GlobalSetting.LauncherName);
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
    }
}