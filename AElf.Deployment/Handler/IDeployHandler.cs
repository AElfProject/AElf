using AElf.Deployment.Models;

namespace AElf.Deployment.Handler
{
    public interface IDeployHandler
    {
        void Excute(DeployType type, string chainId, DeployArg arg = null);
    }
}