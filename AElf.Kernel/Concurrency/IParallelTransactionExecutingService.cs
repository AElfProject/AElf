using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
	public interface IParallelTransactionExecutingService
    {
		Task<List<TransactionResult>> ExecuteAsync(List<ITransaction> transactions, Hash chainId);
    }
}
