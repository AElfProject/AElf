using AElf.Management.Models;

namespace AElf.Management.Commands
{
    public interface IDeployCommand
    {
        void Action(DeployArg arg);
    }
}