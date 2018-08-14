using AElf.Management.Models;

namespace AElf.Management.Command
{
    public interface IDeployCommand
    {
        void Action(string chainId, DeployArg arg);
    }
}