using System;
using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Services
{
    /// <summary>
    /// Create a new chain never existing
    /// </summary>
    public interface IChainCreationService
    {
        Task<IChain> CreateNewChainAsync(Hash chainId, SmartContractRegistration smartContractZero);
    }
}