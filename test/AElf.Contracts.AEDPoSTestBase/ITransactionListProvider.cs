using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Contracts.AEDPoSTestBase
{
    public interface ITransactionListProvider
    {
        Task AddTransactionAsync(Transaction transaction);
        Task<List<Transaction>> GetTransactionListAsync();
        Task ResetAsync();
    }
}