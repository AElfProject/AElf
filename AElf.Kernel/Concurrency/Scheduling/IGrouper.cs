using System.Collections.Generic;
using AElf.Kernel.Types;

namespace AElf.Kernel.Concurrency.Scheduling
{
	public interface IGrouper
    {
		List<List<ITransaction>> Process(Hash chainId, List<ITransaction> transactions);
	    List<List<ITransaction>> ProcessWithCoreCount(int totalCores, Hash chainId, List<ITransaction> transactions);
    }
}
