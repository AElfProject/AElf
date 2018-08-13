using AElf.Deployment.Models;

namespace AElf.Deployment
{
    public interface ISideChainService
    {
        void Deploy(string chainId, DeployArg arg);

        void Remove(string chainId);
    }
}