using AElf.Management.Models;

namespace AElf.Management.Handlers
{
    public interface IDeployHandler
    {
        void Execute(DeployType type, string chainId, DeployArg arg = null);
    }
}