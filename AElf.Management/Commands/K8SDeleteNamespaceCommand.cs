using AElf.Management.Helper;
using AElf.Management.Models;
using k8s.Models;

namespace AElf.Management.Commands
{
    public class K8SDeleteNamespaceCommand:IDeployCommand
    {
        public void Action(string chainId, DeployArg arg)
        {
            K8SRequestHelper.DeleteNamespace(new V1DeleteOptions(), chainId);
        }
    }
}