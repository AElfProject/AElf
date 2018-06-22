using System;
using System.Linq;
using System.Collections.Generic;
using Org.BouncyCastle.Security;

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

        public List<List<ITransaction>> Process(Hash chainId, List<ITransaction> transactions)
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
                foreach (var resource in _resourceUsageDetectionService.GetResources(chainId, tx))
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
        
        public List<List<ITransaction>> ProcessWithCoreCount(int totalCores, Hash chainId, List<ITransaction> transactions)
        {
            if (transactions.Count == 0)
            {
                return new List<List<ITransaction>>();
            }

            if (totalCores <= 0)
            {
                throw new InvalidParameterException("Total core count " + totalCores + " is invalid");
            }
            
            
            var sortedUnmergedGroups = Process(chainId, transactions).OrderBy( a=> a.Count).ToList();

            int resGroupCount = totalCores + 2;
            //TODO: group's count can be a little bit more that core count, for now it's a constant, this value can latter make adjustable to deal with special uses
            int mergeThreshold = transactions.Count / (resGroupCount);
            var res = new List<List<ITransaction>>();

            int startIndex = 0, endIndex = res.Count, totalCount = 0;

            while (startIndex <= endIndex)
            {
                var tempList = sortedUnmergedGroups.ElementAt(startIndex);
                while (tempList.Count + sortedUnmergedGroups.ElementAt(endIndex).Count <= mergeThreshold && startIndex < endIndex)
                {
                    tempList.AddRange(sortedUnmergedGroups.ElementAt(endIndex));
                    endIndex--;
                }
                res.Add(tempList);
                totalCount += tempList.Count;
                startIndex++;
            }

            //in case there is a bug 
            if (totalCount != transactions.Count)
            {
                throw new InvalidOperationException("There is a bug in the Grouper, get inconsist transaction count");
            }
            return res;
        }

        public List<ITransaction> NextBalancedGroup(int threshold, ref List<List<ITransaction>> sortedUnmergeList)
        {
            if(sortedUnmergeList.Count == 0) return new List<ITransaction>();
            
            var res = sortedUnmergeList.First();
            sortedUnmergeList.RemoveAt(0);
            while (res.Count + sortedUnmergeList.Last().Count <= threshold)
            {
                res.AddRange(sortedUnmergeList.Last());
                sortedUnmergeList.RemoveAt(sortedUnmergeList.Count-1);
            }

            return res;
        }
    }
}
