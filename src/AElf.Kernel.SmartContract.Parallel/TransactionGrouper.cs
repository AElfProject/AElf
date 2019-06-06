using System.Collections.Generic;
using System.Diagnostics;
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

        public async Task<(List<List<Transaction>>, List<Transaction>)> GroupAsync(List<Transaction> transactions)
        {
            var chainContext = await GetChainContextAsync();
            if (chainContext == null)
            {
                return (new List<List<Transaction>>(), transactions);
            }
            
            Logger.LogTrace($"Entered GroupAsync");

            var groups = new List<List<Transaction>>();
            var parallelizables = new List<(Transaction, TransactionResourceInfo)>();
            var nonParallelizables = new List<Transaction>();
            List<Transaction> toBeGrouped;
            if (transactions.Count > _options.MaxTransactions)
            {
                nonParallelizables.AddRange(transactions.GetRange(_options.MaxTransactions, 
                    transactions.Count - _options.MaxTransactions));

                toBeGrouped = transactions.GetRange(0, _options.MaxTransactions);
            }
            else
            {
                toBeGrouped = transactions;
            }

            using (var cts = new CancellationTokenSource(_options.GroupingTimeOut))
            {
                Logger.LogTrace($"Extracting resources for transactions.");
                var txsWithResources = await _resourceExtractionService.GetResourcesAsync(chainContext, toBeGrouped, cts.Token);
                Logger.LogTrace($"Completed resource extraction.");
                
                foreach (var twr in txsWithResources)
                {
                    // If timed out at this point, return all transactions as non-parallelizable
                    if (cts.IsCancellationRequested)
                    {
                        nonParallelizables.Add(twr.Item1);
                        continue;
                    }
                    
                    if (twr.Item2.NonParallelizable)
                    {
                        nonParallelizables.Add(twr.Item1);
                        continue;
                    }
                    
                    if (twr.Item2.Resources.Count == 0)
                    {
                        // groups.Add(new List<Transaction>() {twr.Item1}); // Run in their dedicated group
                        nonParallelizables.Add(twr.Item1);
                        continue;
                    }
                
                    parallelizables.Add(twr);
                }
                
                var groupedTxs = GroupParallelizables(parallelizables);
                Logger.LogTrace($"Completed transaction grouping.");
                
                groups.AddRange(groupedTxs);
            }
            
            Logger.LogTrace($"From {transactions.Count} transactions, grouped into {groups.Count}, " +
                            $"left {nonParallelizables.Count} as non-parallelizable.");

            return (groups, nonParallelizables);
        }

        private List<List<Transaction>> GroupParallelizables(List<(Transaction, TransactionResourceInfo)> txsWithResources)
        {
            var resourceUnionSet = new Dictionary<int, UnionFindNode>();
            var transactionResourceHandle = new Dictionary<Transaction, int>();
            var groups = new List<List<Transaction>>();

            var stopwatch = new Stopwatch();
            stopwatch.Start();
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
            stopwatch.Stop();
            Logger.LogTrace($"## Union resource time: {stopwatch.ElapsedMilliseconds} ms");

            var grouped = new Dictionary<int, List<Transaction>>();

            stopwatch.Reset();
            stopwatch.Start();
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
            
            stopwatch.Stop();
            Logger.LogTrace($"## Group resource time: {stopwatch.ElapsedMilliseconds} ms");
            
            return groups;
        }

        private async Task<ChainContext> GetChainContextAsync()
        {
            var chain = await _blockchainService.GetChainAsync();
            if (chain == null)
            {
                return null;
            }

            var chainContext = new ChainContext()
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            return chainContext;
        }
    }
}