using AElf.Management.Handler;
using AElf.Management.Models;

namespace AElf.Management
{
    public class SideChainService : ISideChainService
    {
        public void Deploy(string chainId, DeployArg arg)
        {
            GetHandler().Execute(DeployType.Deploy, chainId, arg);
        }

        public void Remove(string chainId)
        {
            GetHandler().Execute(DeployType.Remove, chainId);
        }

        private IDeployHandler GetHandler()
        {
            var type = "k8s";
            return DeployHandlerFactory.GetHandler(type);
        }
    }
}