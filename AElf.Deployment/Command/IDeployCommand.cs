using AElf.Deployment.Models;

namespace AElf.Deployment.Command
{
    public interface IDeployCommand
    {
        void Action(DeployArgument arg);
    }
}