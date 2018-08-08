using AElf.Deployment.Models;

namespace AElf.Deployment
{
    public interface IDeployService
    {
        void DeploySideChain(string chainId, DeployArg arg);

        void RemoveSideChain(string chainId);
    }
}