using System;
using System.Threading.Tasks;

namespace AElf.Kernel.Services
{
    /// <summary>
    /// Create a new chain never existing
    /// </summary>
    public interface IChainCreationService
    {
        Task<Chain> CreateNewChainAsync(Hash chainId,Type smartContractType);
    }
}