using System.Threading.Tasks;
using AElf.Management.Helper;
using AElf.Management.Models;
using k8s;
using k8s.Models;

namespace AElf.Management.Commands
{
    public class K8SDeleteNamespaceCommand : IDeployCommand
    {
        public async Task Action(DeployArg arg)
        {
            await K8SRequestHelper.GetClient().DeleteNamespaceAsync(new V1DeleteOptions(), arg.SideChainId);
        }
    }
}