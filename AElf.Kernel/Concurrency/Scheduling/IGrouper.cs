using System.Collections.Generic;

namespace AElf.Kernel.Concurrency.Scheduling
{
	public interface IGrouper
    {
		List<List<ITransaction>> Process(List<ITransaction> transactions);
    }
}
