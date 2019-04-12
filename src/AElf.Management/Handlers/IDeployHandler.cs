using System.Threading.Tasks;
using AElf.Management.Models;

namespace AElf.Management.Handlers
{
    public interface IDeployHandler
    {
        Task Execute(DeployType type, string chainId, DeployArg arg = null);
    }
}