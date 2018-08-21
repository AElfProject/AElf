using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Execution.Scheduling
{
	public interface IGrouper
    {
	    Task<Tuple<List<List<Transaction>>, Dictionary<Transaction, Exception>>> ProcessNaive(Hash chainId, List<Transaction> transactions);

	    Task<Tuple<List<List<Transaction>>, Dictionary<Transaction, Exception>>> ProcessWithCoreCount(GroupStrategy strategy, int totalCores, Hash chainId,
		    List<Transaction> transactions);
    }
}
