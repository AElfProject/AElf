using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel
{
	public interface IParallelTransactionExecutingService
    {
	    int TimeoutMilliSeconds { get; set; };
	    Task<List<TransactionTrace>> ExecuteAsync(List<ITransaction> transactions, Hash chainId);
    }
}
