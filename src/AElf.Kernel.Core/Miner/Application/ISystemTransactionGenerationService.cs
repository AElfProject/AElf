using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.Miner.Application
{
    public interface ISystemTransactionGenerationService
    {
        Task<List<Transaction>> GenerateSystemTransactionsAsync(Address @from, long preBlockHeight, Hash preBlockHash);
    }
}