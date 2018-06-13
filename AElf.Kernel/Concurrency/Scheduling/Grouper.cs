using System.Linq;
using System.Collections.Generic;

namespace AElf.Kernel.Concurrency.Scheduling
{

    /// <summary>
    /// The grouper can be used in both producing subgroup and splitting the job in batch
    /// </summary>
    public class Grouper : IGrouper
    {
        private IResourceUsageDetectionService _resourceUsageDetectionService;

        public Grouper(IResourceUsageDetectionService resourceUsageDetectionService)
        {
            _resourceUsageDetectionService = resourceUsageDetectionService;
        }

        public List<List<ITransaction>> Process(List<ITransaction> transactions)
        {
            var txResourceHandle = new Dictionary<ITransaction, string>();
            if (transactions.Count == 0)
            {
                return new List<List<ITransaction>>();
            }

            Dictionary<string, UnionFindNode> resourceUnionSet = new Dictionary<string, UnionFindNode>();

	        //set up the union find set as the representation of graph and the connected components will be the resulting groups
            foreach (var tx in transactions)
            {
                UnionFindNode first = null;
                foreach (var resource in _resourceUsageDetectionService.GetResources(tx))
                {
                    if (!resourceUnionSet.TryGetValue(resource, out var node))
                    {
                        node = new UnionFindNode();
                        resourceUnionSet.Add(resource, node);
                    }
                    if (first == null)
                    {
                        first = node;
                        txResourceHandle.Add(tx, resource);
                    }
                    else
                    {
                        node.Union(first);
                    }
                }
            }

            Dictionary<int, List<ITransaction>> grouped = new Dictionary<int, List<ITransaction>>();
            List<List<ITransaction>> result = new List<List<ITransaction>>();

            foreach (var tx in transactions)
            {
                if (txResourceHandle.TryGetValue(tx, out var firstResource))
                {
                    int nodeId = resourceUnionSet[firstResource].Find().NodeId;
                    if (!grouped.TryGetValue(nodeId, out var group))
                    {
                        group = new List<ITransaction>();
                        grouped.Add(nodeId, group);
                    }
                    group.Add(tx);
                }
                else
                {
                    //each "resource-free" transaction have its own group
                    result.Add(new List<ITransaction>(){tx});
                }
            }
            result.AddRange(grouped.Values);
            return result;
        }
    }
}
