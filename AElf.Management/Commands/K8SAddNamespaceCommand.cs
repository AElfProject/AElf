using System;
using System.Threading;
using AElf.Management.Helper;
using AElf.Management.Models;
using k8s;
using k8s.Models;

namespace AElf.Management.Commands
{
    public class K8SAddNamespaceCommand:IDeployCommand
    {        
        public void Action(DeployArg arg)
        {
            var retryCount = 0;
            var deployResult = DeployNamespace(arg);
            while (!deployResult)
            {
                if (retryCount > GlobalSetting.DeployRetryTime)
                {
                    throw new Exception("failed to deploy namespace");
                }
                retryCount++;
                Thread.Sleep(3000);
                deployResult = DeployNamespace(arg);
            }
            
            Thread.Sleep(30000);
        }

        private bool DeployNamespace(DeployArg arg)
        {
            
            var body = new V1Namespace
            {
                Metadata = new V1ObjectMeta
                {
                    Name = arg.SideChainId
                }
            };
            
            K8SRequestHelper.GetClient().CreateNamespace(body);

            var ns = K8SRequestHelper.GetClient().ReadNamespace(arg.SideChainId);
            var retryCount = 0;
            while (true)
            {
                if (ns.Status.Phase == "Active")
                {
                    break;
                }

                if (retryCount > GlobalSetting.DeployRetryTime)
                {
                    break;
                }

                retryCount++;
                Thread.Sleep(3000);
                ns = K8SRequestHelper.GetClient().ReadNamespace(arg.SideChainId);
            }

            if (retryCount > GlobalSetting.DeployRetryTime)
            {
                K8SRequestHelper.GetClient().DeleteNamespace(new V1DeleteOptions(), arg.SideChainId);
                return false;
            }

            return true;
        }
    }
}