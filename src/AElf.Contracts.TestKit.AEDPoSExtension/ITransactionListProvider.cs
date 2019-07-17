using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public interface ITransactionListProvider
    {
        Task AddTransactionListAsync(List<Transaction> transactions);
        Task<List<Transaction>> GetTransactionListAsync();
        Task ResetAsync();
    }
}