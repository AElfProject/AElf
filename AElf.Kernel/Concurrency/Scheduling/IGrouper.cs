using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Concurrency.Scheduling
{
	public interface IGrouper
    {
		Task<List<List<ITransaction>>> Process(Hash chainId, List<ITransaction> transactions);
	    Task<List<List<ITransaction>>> ProcessWithCoreCount(int totalCores, Hash chainId, List<ITransaction> transactions);
    }
}
