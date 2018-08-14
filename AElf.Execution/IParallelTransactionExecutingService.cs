using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.SmartContract;

namespace AElf.Execution
{
	public interface IParallelTransactionExecutingService
    {
	    int TimeoutMilliSeconds { get; set; }
	    Task<List<TransactionTrace>> ExecuteAsync(List<Transaction> transactions, Hash chainId);
    }
}
