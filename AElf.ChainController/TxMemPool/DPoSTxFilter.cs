using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Common;
using AElf.Configuration;
using Easy.MessageHub;
using NLog;

namespace AElf.ChainController.TxMemPool
{
    // ReSharper disable InconsistentNaming
    public class DPoSTxFilter
    {
        private readonly Round _currentRoundInfo;
        private readonly Address _myAddress;
        private Func<List<Transaction>, List<Transaction>> _txFilter;

        private readonly ILogger _logger;
        
        private readonly Func<List<Transaction>, List<Transaction>> _generatedByMe = list =>
        {
            var toRemove = new List<Transaction>();
            toRemove.AddRange(list.FindAll(tx => tx.From != Address.LoadHex(NodeConfig.Instance.NodeAccount)));
            return toRemove;
        };
        
        /// <summary>
        /// If tx pool contains more than ore InitializeAElfDPoS tx:
        /// Keep the latest one.
        /// </summary>
        private readonly Func<List<Transaction>, List<Transaction>> _oneInitialTx = list =>
        {
            var toRemove = new List<Transaction>();
            var count = list.Count(tx => tx.MethodName == ConsensusBehavior.InitializeAElfDPoS.ToString());
            if (count > 1)
            {
                toRemove.AddRange(
                    list.FindAll(tx => tx.MethodName == ConsensusBehavior.InitializeAElfDPoS.ToString())
                        .OrderBy(tx => tx.Time).Take(count - 1));
            }

            toRemove.AddRange(
                list.FindAll(tx => tx.MethodName != ConsensusBehavior.InitializeAElfDPoS.ToString()));

            if (count == 0)
            {
                Console.WriteLine("No InitializeAElfDPoS tx in pool.");
            }

            return toRemove;
        };

        private readonly Func<List<Transaction>, List<Transaction>> _onePublishOutValueTx = list =>
        {
            var toRemove = new List<Transaction>();
            var count = list.Count(tx => tx.MethodName == ConsensusBehavior.PublishOutValueAndSignature.ToString());
            if (count > 1)
            {
                toRemove.AddRange(
                    list.FindAll(tx => tx.MethodName == ConsensusBehavior.PublishOutValueAndSignature.ToString())
                        .OrderBy(tx => tx.Time).Take(count - 1));
            }
            
            toRemove.AddRange(
                list.FindAll(tx => tx.MethodName != ConsensusBehavior.PublishOutValueAndSignature.ToString()));
            
            if (count == 0)
            {
                Console.WriteLine("No PublishOutValueAndSignature tx in pool.");
            }

            return toRemove;
        };
        
        private readonly Func<List<Transaction>, List<Transaction>> _oneUpdateAElfDPoSTx = list =>
        {
            var toRemove = new List<Transaction>();
            var count = list.Count(tx => tx.MethodName == ConsensusBehavior.UpdateAElfDPoS.ToString());
            if (count > 1)
            {
                toRemove.AddRange(
                    list.FindAll(tx => tx.MethodName == ConsensusBehavior.UpdateAElfDPoS.ToString())
                        .OrderBy(tx => tx.Time).Take(count - 1));
            }

            toRemove.AddRange(
                list.FindAll(tx =>
                    tx.MethodName != ConsensusBehavior.UpdateAElfDPoS.ToString() &&
                    tx.MethodName != ConsensusBehavior.PublishInValue.ToString()));
            
            if (count == 0)
            {
                Console.WriteLine("No UpdateAElfDPoS tx in pool.");
            }

            return toRemove;
        };

        public DPoSTxFilter()
        {
            _myAddress = Address.LoadHex(NodeConfig.Instance.NodeAccount);
            
            MessageHub.Instance.Subscribe<ConsensusStateChanged>(inState =>
            {
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
            });

            _logger = LogManager.GetLogger(nameof(DPoSTxFilter));
        }

        public List<Transaction> Execute(List<Transaction> txs)
        {
            _logger?.Trace("Before");
            PrintTxList(txs);
            
            var removeFromTxPool = new List<Transaction>();

            var filterList = _txFilter.GetInvocationList();
            foreach (var @delegate in filterList)
            {
                var filter = (Func<List<Transaction>, List<Transaction>>) @delegate;
                try
                {
                    var toRemove = filter(txs);
                    removeFromTxPool.AddRange(toRemove);
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

            _logger?.Trace("After");
            PrintTxList(txs);

            return removeFromTxPool;
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