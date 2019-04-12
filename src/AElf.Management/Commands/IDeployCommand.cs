using System.Threading.Tasks;
using AElf.Management.Models;

namespace AElf.Management.Commands
{
    public interface IDeployCommand
    {
        Task Action(DeployArg arg);
    }
}