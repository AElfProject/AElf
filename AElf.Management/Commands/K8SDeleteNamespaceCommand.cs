using AElf.Management.Helper;
using AElf.Management.Models;
using k8s;
using k8s.Models;

namespace AElf.Management.Commands
{
    public class K8SDeleteNamespaceCommand:IDeployCommand
    {
        public void Action(DeployArg arg)
        {
            K8SRequestHelper.GetClient().DeleteNamespace(new V1DeleteOptions(), arg.SideChainId);
        }
    }
}