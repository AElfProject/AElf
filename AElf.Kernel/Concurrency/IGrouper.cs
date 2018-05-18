using System.Collections.Generic;

namespace AElf.Kernel.Concurrency
{
	public interface IGrouper
    {
		List<List<Transaction>> Process(List<Transaction> transactions);
    }
}
