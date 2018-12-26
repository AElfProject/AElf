using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.Management.Helper;
using AElf.Management.Models;
using k8s;
using k8s.Models;

namespace AElf.Management.Commands
{
    public class K8SAddNamespaceCommand : IDeployCommand
    {
        public async Task Action(DeployArg arg)
        {
            var retryCount = 0;
            var deployResult = await DeployNamespace(arg);
            while (!deployResult)
            {
                if (retryCount > GlobalSetting.DeployRetryTime)
                {
                    throw new Exception("failed to deploy namespace");
                }

                retryCount++;
                Thread.Sleep(3000);
                deployResult = await DeployNamespace(arg);
            }

            Thread.Sleep(30000);
        }

        private async Task<bool> DeployNamespace(DeployArg arg)
        {
            var body = new V1Namespace
            {
                Metadata = new V1ObjectMeta
                {
                    Name = arg.SideChainId
                }
            };

            await K8SRequestHelper.GetClient().CreateNamespaceAsync(body);

            var ns = await K8SRequestHelper.GetClient().ReadNamespaceAsync(arg.SideChainId);
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
                ns = await K8SRequestHelper.GetClient().ReadNamespaceAsync(arg.SideChainId);
            }

            if (retryCount > GlobalSetting.DeployRetryTime)
            {
                await K8SRequestHelper.GetClient().DeleteNamespaceAsync(new V1DeleteOptions(), arg.SideChainId);
                return false;
            }

            return true;
        }
    }
}