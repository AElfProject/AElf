using AElf.Deployment.Models;

namespace AElf.Deployment.Handler
{
    public interface IDeployHandler
    {
        void Deploy(DeployArgument arg);
    }
}