using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.ChainController.Application
{
    /// <summary>
    /// Create a new chain never existing
    /// </summary>
    public interface IChainCreationService
    {
        Task<Chain> CreateNewChainAsync(IEnumerable<Transaction> genesisTransactions);
    }
}