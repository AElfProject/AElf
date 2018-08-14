using System.Collections.Generic;
using AElf.Management.Helper;
using AElf.Management.Models;
using k8s.Models;

namespace AElf.Management.Command
{
    public class K8SAddLauncherCommand : IDeployCommand
    {
        private const string ServiceName = "service-launcher";
        private const string DeploymentName = "deploy-launcher";
        private const int NodePort = 30800;
        private const int RpcPort = 30600;
        private const int ActorPort = 32550;

        public void Action(string chainId, DeployArg arg)
        {
            //AddService(chainId, arg);
            AddDeployment(chainId, arg);
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
                    Type = "NodePort",
                    Ports = new List<V1ServicePort>
                    {
                        new V1ServicePort(NodePort, "node-port", NodePort, "TCP", NodePort),
                        new V1ServicePort(RpcPort, "rpc-port", RpcPort, "TCP", RpcPort)
                    },
                    Selector = new Dictionary<string, string>
                    {
                        {"name", DeploymentName}
                    }
                }
            };

            K8SRequestHelper.CreateNamespacedService(body, chainId);
        }

        private void AddDeployment(string chainId, DeployArg arg)
        {
            var body = new Extensionsv1beta1Deployment
            {
                ApiVersion = "extensions/v1beta1",
                Kind = "Deployment",
                Metadata = new V1ObjectMeta
                {
                    Name = DeploymentName,
                    Labels = new Dictionary<string, string> {{"name", DeploymentName}}
                },

                Spec = new Extensionsv1beta1DeploymentSpec
                {
//                    Selector = new V1LabelSelector {MatchLabels = new Dictionary<string, string> {{"name", DeploymentName}}},
                    Replicas = 1,
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
                                    Image = "aelf/node",
                                    Ports = new List<V1ContainerPort>
                                    {
                                        new V1ContainerPort(NodePort),
                                        new V1ContainerPort(RpcPort)
                                    },
                                    Command = new List<string> {"dotnet", "AElf.Launcher.dll"},
                                    Args = new List<string>
                                    {
                                        "--mine.enable",
                                        "true",
                                        "--rpc.port",
                                        RpcPort.ToString(),
                                        "--node.account",
                                        arg.MainChainAccount,
                                        "--node.port",
                                        NodePort.ToString(),
                                        "--actor.port",
                                        ActorPort.ToString(),
                                        "--node.accountpassword",
                                        arg.AccountPassword,
                                        "--dpos.generator",
                                        "true",
                                        "--chain.new",
                                        "true"
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
                                    ConfigMap = new V1ConfigMapVolumeSource {Name = "config-keys"}
                                }
                            }
                        }
                    }
                }

            };

            var result = K8SRequestHelper.CreateNamespacedDeployment3(body, chainId);
        }
    }
}