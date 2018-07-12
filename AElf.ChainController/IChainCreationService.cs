using System;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.ChainController
{
    /// <summary>
    /// Create a new chain never existing
    /// </summary>
    public interface IChainCreationService
    {
        Task<IChain> CreateNewChainAsync(Hash chainId, SmartContractRegistration smartContractZero);
        Hash GenesisContractHash(Hash chainId);
    }
}