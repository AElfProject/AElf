using System;
using System.Collections.Generic;
using System.Linq;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Configuration;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Types.Transaction;
using Easy.MessageHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Miner.TxMemPool
{
    // ReSharper disable InconsistentNaming
    public class TransactionFilter
    {
        //TODO: should change to an interface like ITransactionFilter
        private Func<List<Transaction>, ILogger, List<Transaction>> _txFilter;

        private delegate int WhoIsFirst(Transaction t1, Transaction t2);

        private static readonly WhoIsFirst IsFirst = (t1, t2) => t1.Time.Nanos > t2.Time.Nanos ? -1 : 1;
        public ILogger<TransactionFilter> Logger {get;set;}

        private static readonly List<string> _latestTxs = new List<string>();

        private readonly Func<List<Transaction>, ILogger, List<Transaction>> _generatedByMe = (list, logger) =>
        {
            var toRemove = new List<Transaction>();
            toRemove.AddRange(list.FindAll(tx => tx.From != Address.Parse(NodeConfig.Instance.NodeAccount)));
            return toRemove;
        };
        
        private readonly Func<List<Transaction>, ILogger, List<Transaction>> _firstCrossChainTxnGeneratedByMe = (list, logger) =>
        {
            var toRemove = new List<Transaction>();
            
            // remove cross chain transaction from others
            // actually this should be empty, because this transaction type won't be broadcast  
            var crossChainTxnsFromOthers = list.FindAll(tx =>
                    tx.IsCrossChainIndexingTransaction() && !tx.From.Equals(Address.Parse(NodeConfig.Instance.NodeAccount)))
                .ToList();
            toRemove.AddRange(crossChainTxnsFromOthers);

            var crossChainTxnsFromMe = list.Where(tx => tx.IsCrossChainIndexingTransaction() &&
                tx.From.Equals(Address.Parse(NodeConfig.Instance.NodeAccount))).ToList();
            if (crossChainTxnsFromMe.Count <= 1)
                return toRemove;
            
            // transaction indexing side chain
            // sort txns with timestamp
            var indexingSideChainTxns =
                crossChainTxnsFromMe.Where(t => t.IsIndexingSideChainTransaction()).ToList();
            indexingSideChainTxns.Sort((t1, t2) => IsFirst(t1, t2));
            var firstIndexingSideChainTxn= indexingSideChainTxns.FirstOrDefault();
            // only reserve first txn
            if (firstIndexingSideChainTxn != null)
                toRemove.AddRange(indexingSideChainTxns.FindAll(t => !t.Equals(firstIndexingSideChainTxn)));
            
            // transaction indexing parent chain
            var indexingParentChainTxns =
                crossChainTxnsFromMe.Where(t => t.IsIndexingParentChainTransaction()).ToList();
            indexingParentChainTxns.Sort((t1, t2) => IsFirst(t1, t2));
            var firstIndexingParentChainTxn = indexingParentChainTxns.FirstOrDefault();
            // only reserve first txn
            if (firstIndexingParentChainTxn != null)
                toRemove.AddRange(indexingParentChainTxns.FindAll(t => !t.Equals(firstIndexingParentChainTxn)));
            return toRemove;
        };
        
        /// <summary>
        /// If tx pool contains more than ore InitializeAElfDPoS tx:
        /// Keep the latest one.
        /// </summary>
        private readonly Func<List<Transaction>, ILogger, List<Transaction>> _oneInitialTx = (list, logger) =>
        {
            var toRemove = new List<Transaction>();
            var count = list.Count(tx => tx.MethodName == ConsensusBehavior.InitialTerm.ToString());
            if (count > 1)
            {
                toRemove.AddRange(list.FindAll(tx => _latestTxs.All(id => id != tx.GetHash().ToHex())));
            }

            _latestTxs.Clear();

            toRemove.AddRange(
                list.FindAll(tx => tx.MethodName != ConsensusBehavior.InitialTerm.ToString()));

            if (count == 0)
            {
                logger.LogWarning("No InitializeAElfDPoS tx in pool.");
            }

            return toRemove;
        };

        private readonly Func<List<Transaction>, ILogger, List<Transaction>> _onePublishOutValueTx = (list, logger) =>
        {
            var toRemove = new List<Transaction>();
            var count = list.Count(tx => tx.MethodName == ConsensusBehavior.PackageOutValue.ToString());
            if (count > 1)
            {
                toRemove.AddRange(list.FindAll(tx => _latestTxs.All(id => id != tx.GetHash().ToHex())));
            }

            _latestTxs.Clear();

            toRemove.AddRange(
                list.FindAll(tx => tx.MethodName != ConsensusBehavior.PackageOutValue.ToString()));

            if (count == 0)
            {
                logger.LogWarning("No PublishOutValueAndSignature tx in pool.");
            }

            return toRemove.Where(t => t.Type == TransactionType.DposTransaction).ToList();
        };

        private readonly Func<List<Transaction>, ILogger, List<Transaction>> _oneNextRoundTxAndOnePublishInValueTxByMe = (list, logger) =>
        {
            var toRemove = new List<Transaction>();
            
            var count = list.Count(tx =>
                tx.MethodName == ConsensusBehavior.NextRound.ToString() ||
                tx.MethodName == ConsensusBehavior.BroadcastInValue.ToString());
            
            if (count == 0)
            {
                logger.LogWarning("No NextRound tx or BroadcastInValue tx in pool.");
                return toRemove;
            }
            
            toRemove.AddRange(list.FindAll(tx => _latestTxs.All(id => id != tx.GetHash().ToHex())));

            _latestTxs.Clear();
            Console.WriteLine("Cleared latest txs.");
            
            var correctRefBlockNumber = list.FirstOrDefault(tx => tx.MethodName == ConsensusBehavior.BroadcastInValue.ToString())?.RefBlockNumber;
            if (correctRefBlockNumber.HasValue)
            {
                toRemove.RemoveAll(tx => tx.RefBlockNumber == correctRefBlockNumber && tx.MethodName == ConsensusBehavior.BroadcastInValue.ToString());
            }
            
            toRemove.AddRange(
                list.FindAll(tx =>
                    tx.MethodName != ConsensusBehavior.NextRound.ToString() &&
                    tx.MethodName != ConsensusBehavior.BroadcastInValue.ToString()));

            return toRemove.Where(t => t.Type == TransactionType.DposTransaction).ToList();
        };
        
        private readonly Func<List<Transaction>, ILogger, List<Transaction>> _oneNextTermTxAndOnePublishInValueTxByMe = (list, logger) =>
        {
            var toRemove = new List<Transaction>();
            
            var count = list.Count(tx =>
                tx.MethodName == ConsensusBehavior.NextTerm.ToString() ||
                tx.MethodName == ConsensusBehavior.BroadcastInValue.ToString());
            
            if (count == 0)
            {
                logger.LogWarning("No NextTerm tx or BroadcastInValue tx in pool.");
                return toRemove;
            }
            
            toRemove.AddRange(list.FindAll(tx => _latestTxs.All(id => id != tx.GetHash().ToHex())));

            _latestTxs.Clear();
            Console.WriteLine("Cleared latest txs.");
            
            var correctRefBlockNumber = list.FirstOrDefault(tx => tx.MethodName == ConsensusBehavior.BroadcastInValue.ToString())?.RefBlockNumber;
            if (correctRefBlockNumber.HasValue)
            {
                toRemove.RemoveAll(tx => tx.RefBlockNumber == correctRefBlockNumber && tx.MethodName == ConsensusBehavior.BroadcastInValue.ToString());
            }
            
            toRemove.AddRange(
                list.FindAll(tx =>
                    tx.MethodName != ConsensusBehavior.NextTerm.ToString() &&
                    tx.MethodName != ConsensusBehavior.BroadcastInValue.ToString()));

            return toRemove.Where(t => t.Type == TransactionType.DposTransaction).ToList();
        };

        public TransactionFilter()
        {
            MessageHub.Instance.Subscribe<DPoSTransactionGenerated>(inTxId =>
            {
                _latestTxs.Add(inTxId.TransactionId);
                Logger.LogTrace($"Added tx: {inTxId.TransactionId}");
            });
            
            MessageHub.Instance.Subscribe<DPoSStateChanged>(inState =>
            {
                if (inState.IsMining)
                {
                    Logger.LogTrace(
                        $"Consensus state changed to {inState.ConsensusBehavior.ToString()}, " +
                        "will reset dpos tx filter.");
                    switch (inState.ConsensusBehavior)
                    {
                        case ConsensusBehavior.InitialTerm:
                            _txFilter = null;
                            _txFilter += _generatedByMe;
                            _txFilter += _oneInitialTx;
                            break;
                        case ConsensusBehavior.PackageOutValue:
                            _txFilter = null;
                            _txFilter += _generatedByMe;
                            _txFilter += _onePublishOutValueTx;
                            break;
                        case ConsensusBehavior.NextRound:
                            _txFilter = null;
                            _txFilter += _oneNextRoundTxAndOnePublishInValueTxByMe;
                            break;
                        case ConsensusBehavior.NextTerm:
                            _txFilter = null;
                            _txFilter += _oneNextTermTxAndOnePublishInValueTxByMe;
                            break;
                    }
                }

                _txFilter += _firstCrossChainTxnGeneratedByMe;
            });
            _txFilter += _firstCrossChainTxnGeneratedByMe;

            Logger= NullLogger<TransactionFilter>.Instance;
        }
        
        public void Execute(List<Transaction> txs)
        {
            var filterList = _txFilter.GetInvocationList();
            foreach (var @delegate in filterList)
            {
                var filter = (Func<List<Transaction>, ILogger, List<Transaction>>) @delegate;
                try
                {
                    var toRemove = filter(txs,Logger);
                    foreach (var transaction in toRemove)
                    {
                        txs.Remove(transaction);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogTrace(e, "Failed to execute dpos txs filter.");
                    throw;
                }
            }
        }

        private void PrintTxList(IEnumerable<Transaction> txs)
        {
            Logger.LogTrace("Txs list:");
            foreach (var transaction in txs)
            {
                Logger.LogTrace($"{transaction.GetHash().ToHex()} - {transaction.MethodName}");
            }
        }
    }

}