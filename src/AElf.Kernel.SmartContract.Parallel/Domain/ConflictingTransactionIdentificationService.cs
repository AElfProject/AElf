using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContract.Parallel.Domain
{
    public class ConflictingTransactionIdentificationService : IConflictingTransactionIdentificationService
    {
        private readonly IResourceExtractionService _resourceExtractionService;
        private readonly IBlockchainService _blockchainService;
        
        public ILogger<ConflictingTransactionIdentificationService> Logger { get; set; }

        public ConflictingTransactionIdentificationService(IResourceExtractionService resourceExtractionService,
            IBlockchainService blockchainService)
        {
            _resourceExtractionService = resourceExtractionService;
            _blockchainService = blockchainService;
            
            Logger = NullLogger<ConflictingTransactionIdentificationService>.Instance;
        }

        public async Task<List<TransactionWithResourceInfo>> IdentifyConflictingTransactionsAsync(IChainContext chainContext,
            List<ExecutionReturnSet> returnSets, List<ExecutionReturnSet> conflictingSets)
        {
            var possibleConflicting = FindPossibleConflictingReturnSets(returnSets, conflictingSets);
            var wrongTxnWithResources = await FindContractOfWrongResourcesAsync(chainContext, possibleConflicting);
            return wrongTxnWithResources;
        }

        private List<ExecutionReturnSet> FindPossibleConflictingReturnSets(List<ExecutionReturnSet> returnSets,
            List<ExecutionReturnSet> conflictingSets)
        {
            var existingKeys = new HashSet<string>(returnSets.SelectMany(rs => rs.StateAccesses.Keys));
            var possibleConflictingKeys = new HashSet<string>(conflictingSets.SelectMany(rs => rs.StateAccesses.Keys));
            possibleConflictingKeys.IntersectWith(existingKeys);
            return returnSets.Concat(conflictingSets)
                .Where(rs => rs.StateAccesses.Any(a => possibleConflictingKeys.Contains(a.Key))).ToList();
        }

        private async Task<List<TransactionWithResourceInfo>> FindContractOfWrongResourcesAsync(IChainContext chainContext,
            List<ExecutionReturnSet> returnSets)
        {
            var transactionIds = returnSets.Select(rs => rs.TransactionId);
            var transactions = await _blockchainService.GetTransactionsAsync(transactionIds);

            var txnWithResources =
                await _resourceExtractionService.GetResourcesAsync(chainContext, transactions, CancellationToken.None);
            txnWithResources =
                txnWithResources.Where(t => t.TransactionResourceInfo.ParallelType == ParallelType.Parallelizable);

            var txnWithResourceList = txnWithResources.ToList();
            var readOnlyKeys = txnWithResourceList.GetReadOnlyPaths().Select(p=>p.ToStateKey()).ToList();
            var returnSetLookup = returnSets.ToDictionary(rs => rs.TransactionId, rs => rs);
            var wrongTxnWithResources = new List<TransactionWithResourceInfo>();
            foreach (var txnWithResource in txnWithResourceList)
            {
                var extracted = new HashSet<string>(txnWithResource.TransactionResourceInfo.WritePaths
                    .Concat(txnWithResource.TransactionResourceInfo.ReadPaths).Select(p => p.ToStateKey()));
                extracted.ExceptWith(readOnlyKeys);
                var actual = GetKeys(returnSetLookup[txnWithResource.Transaction.GetHash()]);
                actual.ExceptWith(extracted);
                if (actual.Count == 0) continue;
                Logger.LogDebug($"Conflict keys:{string.Join(";", actual)}");
                wrongTxnWithResources.Add(txnWithResource);
            }

            return wrongTxnWithResources;
        }

        private HashSet<string> GetKeys(ExecutionReturnSet returnSet)
        {
            return new HashSet<string>(returnSet.StateAccesses.Keys);
        }

    }
}