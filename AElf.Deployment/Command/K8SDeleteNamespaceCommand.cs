using AElf.Deployment.Helper;
using AElf.Deployment.Models;
using k8s.Models;

namespace AElf.Deployment.Command
{
    public class K8SDeleteNamespaceCommand:IDeployCommand
    {
        public void Action(string chainId, DeployArg arg)
        {
            K8SRequestHelper.DeleteNamespace(new V1DeleteOptions(), chainId);
        }
    }
}