using System;
using System.Linq;
using System.Collections.Generic;
using AElf.Kernel.Types;
using NLog;
using Org.BouncyCastle.Security;

namespace AElf.Kernel.Concurrency.Scheduling
{

    /// <summary>
    /// The grouper can be used in both producing subgroup and splitting the job in batch
    /// </summary>
    public class Grouper : IGrouper
    {
        private IResourceUsageDetectionService _resourceUsageDetectionService;
        private ILogger _logger;

        public Grouper(IResourceUsageDetectionService resourceUsageDetectionService, ILogger logger = null)
        {
            _resourceUsageDetectionService = resourceUsageDetectionService;
            _logger = logger;
        }

        //TODO: for testnet we only have a single chain, thus grouper only take care of txList in one chain (hence Process has chainId as parameter)
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
                var resources = _resourceUsageDetectionService.GetResources(chainId, tx);
                //_logger.Info(string.Format("tx {0} have resource [{1}]", tx.From, string.Join(" ||| ", resources)));
                foreach (var resource in resources)
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

            _logger?.Info(string.Format(
                "Grouper on chainId [{0}] group {1} transactions into {2} groups with sizes [{3}]", chainId,
                transactions.Count, result.Count, string.Join(", ", result.Select(a=>a.Count))));
            
            return result;
        }

        public List<List<ITransaction>> ProcessWithCoreCount(int totalCores, Hash chainId, List<ITransaction> transactions)
        {
            return SimpleProcessWithCoreCount(totalCores, chainId, transactions);
        }
        
        /// <summary>
        /// Reblancing the group, this is a simple version where calculate the threshold [= txCount / totalCores] first,
        /// then fetch next biggest group in group result, merge smallest groups untill the transactions count in group reach threshold
        /// 
        /// Drawback of this approach: could produce some unbalance result in some cases:
        ///     Consider group result is [499, 497, 496, 495, 6, 3, 2, 2] with 3 cores
        ///     This func Will produce group in first step: [499, 499, 498, 498, 6], next step will be [1002, 499, 499] which is unbalanced
        /// </summary>
        /// <param name="totalCores"></param>
        /// <param name="chainId"></param>
        /// <param name="transactions"></param>
        /// <returns></returns>
        /// <exception cref="InvalidParameterException"></exception>
        public List<List<ITransaction>> SimpleProcessWithCoreCount(int totalCores, Hash chainId, List<ITransaction> transactions)
        {
            if (transactions.Count == 0)
            {
                return new List<List<ITransaction>>();
            }

            if (totalCores <= 0)
            {
                throw new InvalidParameterException("Total core count " + totalCores + " is invalid");
            }
            
            
            var sortedUnmergedGroups = Process(chainId, transactions).OrderByDescending( a=> a.Count).ToList();

            //TODO: group's count can be a little bit more that core count, for now it's 0, this value can latter make adjustable to deal with special uses
            int resGroupCount = totalCores + 0;
            int mergeThreshold = transactions.Count / (resGroupCount);
            var res = new List<List<ITransaction>>();

            int startIndex = 0, endIndex = sortedUnmergedGroups.Count - 1, totalCount = 0;

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
                _logger.Fatal("There is a bug in the Grouper, get inconsist transaction count, some tx lost");
            }

            if (res.Count > resGroupCount)
            {
                var temp = res.OrderBy(a => a.Count).ToList();
                res.Clear();
                int index;
                var merge = new List<ITransaction>();
                for (index = 0; index <= temp.Count - resGroupCount; index++)
                {
                    merge.AddRange(temp[index]);
                }
                res.Add(merge);
                for (; index < temp.Count; index++)
                {
                    res.Add(temp[index]);
                }
            }
            
            _logger?.Info(string.Format(
                "Grouper on chainId [{0}] merge {1} groups into {2} groups with sizes [{3}]", chainId,
                sortedUnmergedGroups.Count, res.Count, string.Join(", ", res.Select(a=>a.Count))));
            return res;
        }
    }
}
