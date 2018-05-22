using System.Linq;
using System.Collections.Generic;

namespace AElf.Kernel.Concurrency.Scheduling
{
	public class Batcher : IBatcher
	{
		/// <summary>
		/// Batch a group so that transactions that sent by same address can maintains their original order
		/// </summary>
		/// <param name="transactions"></param>
		/// <returns></returns>
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
					// Every time take one transaction from each sender,
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
