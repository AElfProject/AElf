using System.Threading.Tasks;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface ISideChainService
    {
        Task Deploy(DeployArg arg);

        Task Remove(string chainId);
    }
}