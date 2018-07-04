using System;
using System.Collections.Generic;

namespace AElf.Kernel.Concurrency.Scheduling
{
	public interface IGrouper
    {
		List<List<ITransaction>> ProcessNaive(Hash chainId, List<ITransaction> transactions, out Dictionary<ITransaction, Exception> failedTxs);

	    List<List<ITransaction>> ProcessWithCoreCount(GroupStrategy strategy, int totalCores, Hash chainId,
		    List<ITransaction> transactions, out Dictionary<ITransaction, Exception> failedTxs);
    }
}
