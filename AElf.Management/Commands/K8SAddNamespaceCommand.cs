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
        public void Action(string chainId, DeployArg arg)
        {
            var retryCount = 0;
            var deployResult = DeployNamespace(chainId, arg);
            while (!deployResult)
            {
                if (retryCount > GlobalSetting.DeployRetryTime)
                {
                    //throw new Exception("failed to deploy namespace");
                }
                retryCount++;
                Thread.Sleep(3000);
                deployResult = DeployNamespace(chainId, arg);
            }
            
            Thread.Sleep(30000);
        }

        private bool DeployNamespace(string chainId, DeployArg arg)
        {
            
            var body = new V1Namespace
            {
                Metadata = new V1ObjectMeta
                {
                    Name = chainId
                }
            };
            
            K8SRequestHelper.GetClient().CreateNamespace(body);

            var ns = K8SRequestHelper.GetClient().ReadNamespace(chainId);
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
                ns = K8SRequestHelper.GetClient().ReadNamespace(chainId);
            }

            if (retryCount > GlobalSetting.DeployRetryTime)
            {
                K8SRequestHelper.GetClient().DeleteNamespace(new V1DeleteOptions(), chainId);
                return false;
            }

            return true;
        }
    }
}