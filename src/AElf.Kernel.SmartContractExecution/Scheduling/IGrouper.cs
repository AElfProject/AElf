using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContractExecution.Scheduling
{
	public interface IGrouper
    {
	    Task<Tuple<List<List<Transaction>>, Dictionary<Transaction, Exception>>> ProcessNaive(int chainId, List<Transaction> transactions);

	    Task<Tuple<List<List<Transaction>>, Dictionary<Transaction, Exception>>> ProcessWithCoreCount(GroupStrategy strategy, int totalCores, int chainId,
		    List<Transaction> transactions);
    }
}
