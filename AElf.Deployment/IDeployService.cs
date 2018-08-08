using AElf.Deployment.Models;

namespace AElf.Deployment
{
    public interface IDeployService
    {
        void DeploySideChain(DeployArgument arg);
    }
}