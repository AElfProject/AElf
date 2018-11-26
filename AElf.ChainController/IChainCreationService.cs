using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.ChainController
{
    /// <summary>
    /// Create a new chain never existing
    /// </summary>
    public interface IChainCreationService
    {
        Task<IChain> CreateNewChainAsync(Hash chainId, List<SmartContractRegistration> smartContractZeros);
    }
}