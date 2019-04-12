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
    public class K8SAddLauncherServiceCommand : IDeployCommand
    {
        public async Task Action(DeployArg arg)
        {
            var result = await AddService(arg);

            if (!result)
            {
                throw new Exception("failed to deploy launcher service");
            }
        }

        private async Task<bool> AddService(DeployArg arg)
        {
            var body = new V1Service
            {
                Metadata = new V1ObjectMeta
                {
                    Name = GlobalSetting.LauncherServiceName,
                    Labels = new Dictionary<string, string>
                    {
                        {"name", GlobalSetting.LauncherServiceName}
                    }
                },
                Spec = new V1ServiceSpec
                {
                    Type = "LoadBalancer",
                    Ports = new List<V1ServicePort>
                    {
                        new V1ServicePort(GlobalSetting.NodePort, "node-port", null, "TCP", GlobalSetting.NodePort),
                        new V1ServicePort(GlobalSetting.RpcPort, "rpc-port", null, "TCP", GlobalSetting.RpcPort),
                        new V1ServicePort(GlobalSetting.GrpcPort, "grpc-port", null, "TCP", GlobalSetting.GrpcPort)
                    },
                    Selector = new Dictionary<string, string>
                    {
                        {"name", GlobalSetting.LauncherName}
                    }
                }
            };

            var result = await K8SRequestHelper.GetClient().CreateNamespacedServiceAsync(body, arg.SideChainId);

            var service = await K8SRequestHelper.GetClient().ReadNamespacedServiceAsync(result.Metadata.Name, arg.SideChainId);
            var retryGetCount = 0;
            while (true)
            {
                arg.LauncherArg.ClusterIp = service.Spec.ClusterIP;
                if (!string.IsNullOrWhiteSpace(arg.LauncherArg.ClusterIp))
                {
                    break;
                }

                if (retryGetCount > GlobalSetting.DeployRetryTime)
                {
                    return false;
                }

                retryGetCount++;
                Thread.Sleep(3000);
                service = await K8SRequestHelper.GetClient().ReadNamespacedServiceAsync(result.Metadata.Name, arg.SideChainId);
            }

            return true;
        }
    }
}