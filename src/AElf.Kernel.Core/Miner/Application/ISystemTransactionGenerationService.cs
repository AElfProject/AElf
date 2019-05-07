using System.Threading.Tasks;
using System.Collections.Generic;

namespace AElf.Kernel.Miner.Application
{
    public interface ISystemTransactionGenerationService
    {
        Task<List<Transaction>> GenerateSystemTransactions(Address from, long preBlockHeight, Hash preBlockHash);
    }
}