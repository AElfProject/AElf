using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel
{
    public interface ITransactionGrouper
    {
        Task<GroupedTransactions> GroupAsync(IChainContext chainContext,
            List<Transaction> transactions);
    }
    
    public class GroupedTransactions
    {
        public List<List<Transaction>> Parallelizables = new List<List<Transaction>>();
        public List<Transaction> NonParallelizables = new List<Transaction>();
    }
}
