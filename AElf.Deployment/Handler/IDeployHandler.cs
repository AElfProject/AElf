using AElf.Deployment.Models;

namespace AElf.Deployment.Handler
{
    public interface IDeployHandler
    {
        void Execute(DeployType type, string chainId, DeployArg arg = null);
    }
}