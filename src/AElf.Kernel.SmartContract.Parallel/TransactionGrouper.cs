using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class GrouperOptions
    {
        public int GroupingTimeOut { get; set; } = 500; // ms
        public int MaxTransactions { get; set; } = int.MaxValue;   // Maximum transactions to group
    }

    public class TransactionGrouper : ITransactionGrouper, ISingletonDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IResourceExtractionService _resourceExtractionService;
        private GrouperOptions _options;
        public ILogger<TransactionGrouper> Logger { get; set; }

        public TransactionGrouper(IBlockchainService blockchainService,
            IResourceExtractionService resourceExtractionService, IOptionsSnapshot<GrouperOptions> options)
        {
            _blockchainService = blockchainService;
            _resourceExtractionService = resourceExtractionService;
            _options = options.Value;
            Logger = NullLogger<TransactionGrouper>.Instance;
        }

        public async Task<GroupedTransactions> GroupAsync(IChainContext chainContext,List<Transaction> transactions)
        {
            Logger.LogTrace($"Entered GroupAsync");

            var groupedTransactions = new GroupedTransactions();

            List<Transaction> toBeGrouped;
            if (transactions.Count > _options.MaxTransactions)
            {
                groupedTransactions.NonParallelizables.AddRange(
                    transactions.GetRange(_options.MaxTransactions, transactions.Count - _options.MaxTransactions));

                toBeGrouped = transactions.GetRange(0, _options.MaxTransactions);
            }
            else
            {
                toBeGrouped = transactions;
            }

            using (var cts = new CancellationTokenSource(_options.GroupingTimeOut))
            {
                var parallelizables = new List<(Transaction, TransactionResourceInfo)>();
                
                Logger.LogTrace($"Extracting resources for transactions.");
                var txsWithResources = await _resourceExtractionService.GetResourcesAsync(chainContext, toBeGrouped, cts.Token);
                Logger.LogTrace($"Completed resource extraction.");
                
                foreach (var twr in txsWithResources)
                {
                    // If timed out at this point, return all transactions as non-parallelizable
                    if (cts.IsCancellationRequested)
                    {
                        groupedTransactions.NonParallelizables.Add(twr.Item1);
                        continue;
                    }
                    
                    if (twr.Item2.NonParallelizable)
                    {
                        groupedTransactions.NonParallelizables.Add(twr.Item1);
                        continue;
                    }
                    
                    if (twr.Item2.Resources.Count == 0)
                    {
                        // groups.Add(new List<Transaction>() {twr.Item1}); // Run in their dedicated group
                        groupedTransactions.NonParallelizables.Add(twr.Item1);
                        continue;
                    }
                
                    parallelizables.Add(twr);
                }

                groupedTransactions.Parallelizables.AddRange(GroupParallelizables(parallelizables));
                
                Logger.LogTrace($"Completed transaction grouping.");
            }
            
            Logger.LogTrace($"From {transactions.Count} transactions, grouped into " +
                            $"{groupedTransactions.Parallelizables.Count} groups, left" +
                            $"{groupedTransactions.NonParallelizables.Count} as non-parallelizable transactions.");

            return groupedTransactions;
        }

        private List<List<Transaction>> GroupParallelizables(List<(Transaction, TransactionResourceInfo)> txsWithResources)
        {
            var resourceUnionSet = new Dictionary<int, UnionFindNode>();
            var transactionResourceHandle = new Dictionary<Transaction, int>();
            var groups = new List<List<Transaction>>();
            
            foreach (var txWithResource in txsWithResources)
            {
                UnionFindNode first = null;
                var transaction = txWithResource.Item1;
                var transactionResourceInfo = txWithResource.Item2;

                // Add resources to disjoint-set, later each resource will be connected to a node id, which will be our group id
                foreach (var resource in transactionResourceInfo.Resources)
                {
                    if (!resourceUnionSet.TryGetValue(resource, out var node))
                    {
                        node = new UnionFindNode();
                        resourceUnionSet.Add(resource, node);
                    }

                    if (first == null)
                    {
                        first = node;
                        transactionResourceHandle.Add(transaction, resource);
                    }
                    else
                    {
                        node.Union(first);
                    }
                }
            }

            var grouped = new Dictionary<int, List<Transaction>>();

            foreach (var txWithResource in txsWithResources)
            {
                var transaction = txWithResource.Item1;
                if (!transactionResourceHandle.TryGetValue(transaction, out var firstResource))
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
    }
}