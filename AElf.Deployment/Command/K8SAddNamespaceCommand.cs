using AElf.Deployment.Helper;
using AElf.Deployment.Models;
using k8s.Models;

namespace AElf.Deployment.Command
{
    public class K8SAddNamespaceCommand:IDeployCommand
    {
        public void Action(string chainId, DeployArg arg)
        {
            var body = new V1Namespace
            {
                Metadata = new V1ObjectMeta
                {
                    Name = chainId
                }
            };
            
            K8SRequestHelper.CreateNamespace(body);
        }
    }
}