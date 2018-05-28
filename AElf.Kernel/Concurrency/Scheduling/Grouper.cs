using System.Linq;
using System.Collections.Generic;

namespace AElf.Kernel.Concurrency.Scheduling
{
	public class Grouper : IGrouper
	{
		public List<List<ITransaction>> Process(List<ITransaction> transactions)
		{
			if (transactions.Count == 0)
			{
				return new List<List<ITransaction>>();
			}

			Dictionary<Hash, UnionFindNode> accountUnionSet = new Dictionary<Hash, UnionFindNode>();

			//set up the union find set

			foreach (var tx in transactions)
			{
				UnionFindNode first = null;
				foreach (var hash in tx.GetResources())
				{
					if (!accountUnionSet.TryGetValue(hash, out var node))
					{
						node = new UnionFindNode();
						accountUnionSet.Add(hash, node);
					}
					if (first == null)
					{
						first = node;
					}
					else
					{
						node.Union(first);
					}
				}
			}

			Dictionary<int, List<ITransaction>> grouped = new Dictionary<int, List<ITransaction>>();

			foreach (var tx in transactions)
			{
				int nodeId = accountUnionSet[tx.From].Find().NodeId;
				if (!grouped.TryGetValue(nodeId, out var group))
				{
					group = new List<ITransaction>();
					grouped.Add(nodeId, group);
				}
				group.Add(tx);
			}

			return grouped.Values.ToList();
		}
	}
}
