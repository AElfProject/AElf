using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface IChainService
    {
        Task<List<ChainResult>> GetAllChains();

        Task DeployMainChain(DeployArg arg);

        Task RemoveMainChain(string chainId);
    }
}