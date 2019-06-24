using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel
{
    public interface ITransactionGrouper
    {
        Task<(List<List<Transaction>>, List<Transaction>)> GroupAsync(List<Transaction> transactions);
    }
}
