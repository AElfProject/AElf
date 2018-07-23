using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.ChainController.Execution
{
	public interface IGrouper
    {
	    Task<Tuple<List<List<ITransaction>>, Dictionary<ITransaction, Exception>>> ProcessNaive(Hash chainId, List<ITransaction> transactions);

	    Task<Tuple<List<List<ITransaction>>, Dictionary<ITransaction, Exception>>> ProcessWithCoreCount(GroupStrategy strategy, int totalCores, Hash chainId,
		    List<ITransaction> transactions);
    }
}
