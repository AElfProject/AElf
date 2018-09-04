using AElf.Management.Handlers;
using AElf.Management.Interfaces;
using AElf.Management.Models;

namespace AElf.Management.Services
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
            return DeployHandlerFactory.GetHandler();
        }
    }
}