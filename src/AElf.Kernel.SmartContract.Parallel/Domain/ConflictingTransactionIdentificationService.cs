using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel.Domain
{
    public class ConflictingTransactionIdentificationService : IConflictingTransactionIdentificationService
    {
        private readonly IResourceExtractionService _resourceExtractionService;
        private readonly IBlockchainService _blockchainService;

        public ConflictingTransactionIdentificationService(IResourceExtractionService resourceExtractionService,
            IBlockchainService blockchainService)
        {
            _resourceExtractionService = resourceExtractionService;
            _blockchainService = blockchainService;
        }

        public async Task<List<Transaction>> IdentifyConflictingTransactionsAsync(IChainContext chainContext,
            List<ExecutionReturnSet> returnSets, List<ExecutionReturnSet> conflictingSets)
        {
            var possibleConflicting = FindPossibleConflictingReturnSets(returnSets, conflictingSets);
            var wrong = await FindContractOfWrongResourcesAsync(chainContext, possibleConflicting);
            return wrong;
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

        private async Task<List<Transaction>> FindContractOfWrongResourcesAsync(IChainContext chainContext,
            List<ExecutionReturnSet> returnSets)
        {
            var transactionIds = returnSets.Select(rs => rs.TransactionId);
            var transactions = await _blockchainService.GetTransactionsAsync(transactionIds);

            var txnWithResources =
                await _resourceExtractionService.GetResourcesAsync(chainContext, transactions, CancellationToken.None);

            var returnSetLookup = returnSets.ToDictionary(rs => rs.TransactionId, rs => rs);
            var wrongTxns = new List<Transaction>();
            foreach (var txnWithResource in txnWithResources)
            {
                var extracted = new HashSet<string>(txnWithResource.Item2.Paths.Select(p => p.ToStateKey()));
                var actual = GetKeys(returnSetLookup[txnWithResource.Item1.GetHash()]);
                actual.ExceptWith(extracted);
                if (actual.Count > 0)
                {
                    wrongTxns.Add(txnWithResource.Item1);
                }
            }

            return wrongTxns;
        }

        private HashSet<string> GetKeys(ExecutionReturnSet returnSet)
        {
            return new HashSet<string>(returnSet.StateAccesses.Keys);
        }

    }
}