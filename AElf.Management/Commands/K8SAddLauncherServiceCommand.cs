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
    public class K8SAddLauncherServiceCommand:IDeployCommand
    {
        public void Action(DeployArg arg)
        {
            var result = AddService(arg);
            
            if (!result)
            {
                throw new Exception("failed to deploy launcher service");
            }
        }
        
        private bool AddService(DeployArg arg)
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

            var result = K8SRequestHelper.GetClient().CreateNamespacedService(body, arg.SideChainId);
            
            var service = K8SRequestHelper.GetClient().ReadNamespacedService(result.Metadata.Name, arg.SideChainId);
            var retryGetCount = 0;
            while (true)
            {
                var ingress = service.Status.LoadBalancer.Ingress;
                if (ingress != null && !string.IsNullOrWhiteSpace(ingress.FirstOrDefault().Ip))
                {
                    arg.LauncherArg.ClusterIp = ingress.FirstOrDefault().Ip;
                    break;
                }

                if (retryGetCount > GlobalSetting.DeployRetryTime)
                {
                    return false;
                }

                retryGetCount++;
                Thread.Sleep(3000);
                service = K8SRequestHelper.GetClient().ReadNamespacedService(result.Metadata.Name, arg.SideChainId);
            }

            return true;
        }
    }
}