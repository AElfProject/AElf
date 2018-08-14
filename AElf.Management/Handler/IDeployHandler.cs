using AElf.Management.Models;

namespace AElf.Management.Handler
{
    public interface IDeployHandler
    {
        void Execute(DeployType type, string chainId, DeployArg arg = null);
    }
}