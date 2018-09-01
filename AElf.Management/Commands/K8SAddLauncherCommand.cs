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
    public class K8SAddLauncherCommand : IDeployCommand
    {
        private const string ServiceName = "service-launcher";
        private const string DeploymentName = "deploy-launcher";
        private const int NodePort = 30800;
        private const int RpcPort = 30600;
        private const int ActorPort = 32550;
        private const int Replicas = 1;

        public void Action(string chainId, DeployArg arg)
        {
            AddService(chainId, arg);

            var addDeployResult = AddDeployment(chainId, arg);

            if (!addDeployResult)
            {
                //throw new Exception("failed to deploy launcher");
            }
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
                    Type = "LoadBalancer",
                    Ports = new List<V1ServicePort>
                    {
                        new V1ServicePort(NodePort, "node-port", null, "TCP", NodePort),
                        new V1ServicePort(RpcPort, "rpc-port", null, "TCP", RpcPort)
                    },
                    Selector = new Dictionary<string, string>
                    {
                        {"name", DeploymentName}
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
                    Name = DeploymentName,
                    Labels = new Dictionary<string, string> {{"name", DeploymentName}}
                },

                Spec = new V1DeploymentSpec
                {
                    Selector = new V1LabelSelector {MatchLabels = new Dictionary<string, string> {{"name", DeploymentName}}},
                    Replicas = Replicas,
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta {Labels = new Dictionary<string, string> {{"name", DeploymentName}}},
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = DeploymentName,
                                    Image = "aelf/node:test",
                                    Ports = new List<V1ContainerPort>
                                    {
                                        new V1ContainerPort(NodePort),
                                        new V1ContainerPort(RpcPort),
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
                                    Command = new List<string> {"dotnet", "AElf.Launcher.dll"},
                                    Args = new List<string>
                                    {
                                        "--mine.enable",
                                        "true",
                                        "--rpc.host",
                                        "0.0.0.0",
                                        "--rpc.port",
                                        RpcPort.ToString(),
                                        "--node.account",
                                        arg.MainChainAccount,
                                        "--node.port",
                                        NodePort.ToString(),
                                        "--actor.host",
                                        "$(POD_IP)",
                                        "--actor.port",
                                        ActorPort.ToString(),
                                        "--node.accountpassword",
                                        arg.AccountPassword,
                                        "--dpos.generator",
                                        arg.LauncherArg.IsConsensusInfoGenerator.ToString(),
                                        "--chain.new",
                                        "true",
                                        "--chain.id",
                                        chainId.Split('-').First(),
                                        "--node.executor",
                                        arg.ManagerArg.IsCluster?"akka":"simple"
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
                                },
                                new V1Volume
                                {
                                    Name = "key",
                                    ConfigMap = new V1ConfigMapVolumeSource
                                    {
                                        Name = "config-keys",
                                        Items = new List<V1KeyToPath>
                                        {
                                            new V1KeyToPath{Key = arg.MainChainAccount+".ak",Path = arg.MainChainAccount+".ak"}
                                        }
                                    }
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
            K8SRequestHelper.GetClient().DeleteCollectionNamespacedPod(chainId, labelSelector: "name=" + DeploymentName);
        }
    }
}