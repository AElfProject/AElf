using System.Linq;
using System.Collections.Generic;

namespace AElf.Kernel.Concurrency
{
	public class Batcher : IBatcher
	{
		public List<List<Transaction>> Process(List<Transaction> transactions)
		{
			if(transactions.Count == 0){
				return new List<List<Transaction>>();
			}

			var enumerators = transactions.GroupBy(x => x.From)
										  .Select(y => y.GetEnumerator())
			                              .ToList();
			var batched = new List<List<Transaction>>();
			try
			{
				while (true)
				{
					// Every time take one from each group,
                    // so that possible parallelization can be done within each batch
					var batch = enumerators.Where(x => x.MoveNext()).Select(x => x.Current).ToList();
					if (batch.Count == 0)
						break;
					batched.Add(batch);
				}

			}
			finally
			{
				foreach (var e in enumerators)
					e.Dispose();
			}

			return batched;
		}
	}
}
