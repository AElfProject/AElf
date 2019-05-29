using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Scheduling;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContractExecution.Parallel
{
    public class TransactionGrouper : ITransactionGrouper
    {
        private IBlockchainService _blockchainService;
        private IResourceExtractionService _resourceExtractionService;
        public ILogger<TransactionGrouper> Logger {get; set;}
        
        public TransactionGrouper(IBlockchainService blockchainService, 
            IResourceExtractionService resourceExtractionService)
        {
            _blockchainService = blockchainService;
            _resourceExtractionService = resourceExtractionService;
            Logger = NullLogger<TransactionGrouper>.Instance;
        }

        public async Task<List<List<Transaction>>> GroupAsync(List<Transaction> transactions)
        {
            var chainContext = await GetChainContextAsync();
            var resourceUnionSet = new Dictionary<int, UnionFindNode>();
            var txResourceHandle = new Dictionary<Transaction, int>();
            var groups = new List<List<Transaction>>();

            // Get the resources for each transaction and add its resources to disjoint-set data structure for grouping
            var nonParallelizable = new List<Transaction>();
            foreach (var transaction in transactions)
            {
                UnionFindNode first = null;
                TransactionResourceInfo txResourceInfo;

                try
                {
                    txResourceInfo = _resourceExtractionService.GetResourcesAsync(chainContext, new[] {transaction}).Result.First();
                }
                catch (Exception e)
                {
                    Logger.LogWarning(e, $"Unable to get resources for transaction {transaction.GetHashCode()}. Won't be executed in parallel.");
                    nonParallelizable.Add(transaction);
                    continue;
                }
                
                // If non-parallelizable, execute with others in same group
                if (txResourceInfo.NonParallelizable)
                {
                    nonParallelizable.Add(transaction);
                    continue;
                }

                // If no resources, execute in its own group
                if (txResourceInfo.Resources.Count == 0)
                {
                    groups.Add(new List<Transaction>() { transaction });
                }

                // Add resources to disjoint-set, later each resource will be connected to a node id, which will be our group id
                foreach (var resource in txResourceInfo.Resources)
                {
                    if (!resourceUnionSet.TryGetValue(resource, out var node))
                    {
                        node = new UnionFindNode();
                        resourceUnionSet.Add(resource, node);
                    }

                    if (first == null)
                    {
                        first = node;
                        txResourceHandle.Add(transaction, resource);
                    }
                    else
                    {
                        node.Union(first);
                    }
                }
            }
            if (nonParallelizable.Count > 0)
                groups.Add(nonParallelizable);
            
            var grouped = new Dictionary<int, List<Transaction>>();

            foreach (var transaction in transactions)
            {
                if (!txResourceHandle.TryGetValue(transaction, out var firstResource)) 
                    continue;
                
                // Node Id will be our group id
                var gId = resourceUnionSet[firstResource].Find().NodeId;
                    
                if (!grouped.TryGetValue(gId, out var gTransactions))
                {
                    gTransactions = new List<Transaction>();
                    grouped.Add(gId, gTransactions);
                }
                
                // Add transaction to its group
                gTransactions.Add(transaction);
            }
            
            groups.AddRange(grouped.Values);

            return groups;
        }

        private async Task<ChainContext> GetChainContextAsync()
        {
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext()
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            return chainContext;
        }
    }
}