using System;
using System.Collections.Generic;
using System.Linq;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Configuration;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.EventMessages;
using Easy.MessageHub;
using NLog;

namespace AElf.Miner.TxMemPool
{
    // ReSharper disable InconsistentNaming
    public class TransactionFilter
    {
        private Func<List<Transaction>, ILogger, List<Transaction>> _txFilter;

        private delegate int WhoIsFirst(Transaction t1, Transaction t2);

        private static readonly WhoIsFirst IsFirst = (t1, t2) => t1.Time.Nanos > t2.Time.Nanos ? -1 : 1;
        private readonly ILogger _logger;

        private static string _latestTx;

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
                tx.Type == TransactionType.CrossChainBlockInfoTransaction &&
                tx.From != Address.Parse(NodeConfig.Instance.NodeAccount)).ToList();
            toRemove.AddRange(crossChainTxnsFromOthers);
            
            var crossChainTxnsFromMe = list.FindAll(tx =>
                tx.Type == TransactionType.CrossChainBlockInfoTransaction &&
                tx.From == Address.Parse(NodeConfig.Instance.NodeAccount)).ToList();
            if (crossChainTxnsFromMe.Count <= 1)
                return toRemove;
            // sort txns with timestamp
            crossChainTxnsFromMe.Sort((t1, t2) => IsFirst(t1, t2));
            var firstTxn = crossChainTxnsFromMe.FirstOrDefault();
            // only reserve first txn
            if (firstTxn != null)
                toRemove.AddRange(list.FindAll(t =>
                    t.Type == TransactionType.CrossChainBlockInfoTransaction && !t.Equals(firstTxn)));
            return toRemove;
        };
        
        /// <summary>
        /// If tx pool contains more than ore InitializeAElfDPoS tx:
        /// Keep the latest one.
        /// </summary>
        private readonly Func<List<Transaction>, ILogger, List<Transaction>> _oneInitialTx = (list, logger) =>
        {
            var toRemove = new List<Transaction>();
            var count = list.Count(tx => tx.MethodName == ConsensusBehavior.InitializeAElfDPoS.ToString());
            if (count > 1)
            {
                toRemove.AddRange(list.FindAll(tx => tx.GetHash().DumpHex() != _latestTx));
            }

            toRemove.AddRange(
                list.FindAll(tx => tx.MethodName != ConsensusBehavior.InitializeAElfDPoS.ToString()));

            if (count == 0)
            {
                logger?.Warn("No InitializeAElfDPoS tx in pool.");
            }

            return toRemove;
        };

        private readonly Func<List<Transaction>, ILogger, List<Transaction>> _onePublishOutValueTx = (list, logger) =>
        {
            var toRemove = new List<Transaction>();
            var count = list.Count(tx => tx.MethodName == ConsensusBehavior.PublishOutValueAndSignature.ToString());
            if (count > 1)
            {
                toRemove.AddRange(list.FindAll(tx => tx.GetHash().DumpHex() != _latestTx));
            }

            toRemove.AddRange(
                list.FindAll(tx => tx.MethodName != ConsensusBehavior.PublishOutValueAndSignature.ToString()));

            if (count == 0)
            {
                logger?.Warn("No PublishOutValueAndSignature tx in pool.");
            }

            return toRemove.Where(t => t.Type == TransactionType.DposTransaction).ToList();
        };

        private readonly Func<List<Transaction>, ILogger, List<Transaction>> _oneUpdateAElfDPoSTx = (list, logger) =>
        {
            var toRemove = new List<Transaction>();
            var count = list.Count(tx => tx.MethodName == ConsensusBehavior.UpdateAElfDPoS.ToString());
            if (count > 1)
            {
                toRemove.AddRange(list.FindAll(tx => tx.GetHash().DumpHex() != _latestTx));
            }

            toRemove.AddRange(
                list.FindAll(tx =>
                    tx.MethodName != ConsensusBehavior.UpdateAElfDPoS.ToString() &&
                    tx.MethodName != ConsensusBehavior.PublishInValue.ToString()));

            if (count == 0)
            {
                logger?.Warn("No UpdateAElfDPoS tx in pool.");
            }

            return toRemove.Where(t => t.Type == TransactionType.DposTransaction).ToList();
        };

        public TransactionFilter()
        {
            MessageHub.Instance.Subscribe<DPoSTransactionGenerated>(inTxId => { _latestTx = inTxId.TransactionId; });
            MessageHub.Instance.Subscribe<DPoSStateChanged>(inState =>
            {
                if (inState.IsMining)
                {
                    _logger?.Trace(
                        $"Consensus state changed to {inState.ConsensusBehavior.ToString()}, " +
                        "will reset dpos tx filter.");
                    switch (inState.ConsensusBehavior)
                    {
                        case ConsensusBehavior.InitializeAElfDPoS:
                            _txFilter = null;
                            _txFilter += _generatedByMe;
                            _txFilter += _oneInitialTx;
                            break;
                        case ConsensusBehavior.PublishOutValueAndSignature:
                            _txFilter = null;
                            _txFilter += _generatedByMe;
                            _txFilter += _onePublishOutValueTx;
                            break;
                        case ConsensusBehavior.UpdateAElfDPoS:
                            _txFilter = null;
                            _txFilter += _oneUpdateAElfDPoSTx;
                            break;
                    }
                }

                _txFilter += _firstCrossChainTxnGeneratedByMe;
            });

            _logger = LogManager.GetLogger(nameof(TransactionFilter));
        }

        public void Execute(List<Transaction> txs)
        {
            var filterList = _txFilter.GetInvocationList();
            foreach (var @delegate in filterList)
            {
                var filter = (Func<List<Transaction>, ILogger, List<Transaction>>) @delegate;
                try
                {
                    var toRemove = filter(txs, _logger);
                    foreach (var transaction in toRemove)
                    {
                        txs.Remove(transaction);
                    }
                }
                catch (Exception e)
                {
                    _logger?.Trace(e, "Failed to execute dpos txs filter.");
                    throw;
                }
            }
        }

        private void PrintTxList(IEnumerable<Transaction> txs)
        {
            _logger?.Trace("Txs list:");
            foreach (var transaction in txs)
            {
                _logger?.Trace($"{transaction.GetHash().DumpHex()} - {transaction.MethodName}");
            }
        }
    }

}