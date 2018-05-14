using System;
using System.Threading.Tasks;

namespace AElf.Kernel.Services
{
    /// <summary>
    /// Create a new chain never existing
    /// </summary>
    public interface IChainCreationService
    {
        Task<IChain> CreateNewChainAsync(Hash chainId,Type smartContractType);
    }
}