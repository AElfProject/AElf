using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.Configuration;
using AElf.Common;
using AElf.Kernel.Managers;

namespace AElf.Kernel
{
    public class TxHub
    {
        private ITransactionManager _transactionManager;
        private ConcurrentDictionary<Hash, TransactionHolder> _allTxns =
            new ConcurrentDictionary<Hash, TransactionHolder>();

        private ConcurrentDictionary<Hash, TransactionHolder> _waiting =
            new ConcurrentDictionary<Hash, TransactionHolder>();

        private ConcurrentDictionary<Hash, TransactionHolder> _validating =
            new ConcurrentDictionary<Hash, TransactionHolder>();

        private ConcurrentDictionary<Hash, TransactionHolder> _validated =
            new ConcurrentDictionary<Hash, TransactionHolder>();

        private ConcurrentDictionary<Hash, TransactionHolder> _invalid =
            new ConcurrentDictionary<Hash, TransactionHolder>();

        private ConcurrentDictionary<ulong, ConcurrentBag<TransactionHolder>> _validatedByRefBlockNumber =
            new ConcurrentDictionary<ulong, ConcurrentBag<TransactionHolder>>();

//        private ConcurrentDictionary<ulong, ConcurrentBag<TransactionHolder>> _invalid = new ConcurrentDictionary<ulong, ConcurrentBag<TransactionHolder>>();
//        private ConcurrentDictionary<Hash, TransactionHolder> _grouping = new ConcurrentDictionary<Hash, TransactionHolder>();
//        private ConcurrentDictionary<Hash, TransactionHolder> _grouped = new ConcurrentDictionary<Hash, TransactionHolder>();
        private ConcurrentDictionary<Hash, TransactionHolder> _executing =
            new ConcurrentDictionary<Hash, TransactionHolder>();

        private ConcurrentDictionary<Hash, TransactionHolder> _executed =
            new ConcurrentDictionary<Hash, TransactionHolder>();

        private ConcurrentDictionary<ulong, ConcurrentBag<TransactionHolder>>  _executedByBlock =
            new ConcurrentDictionary<ulong, ConcurrentBag<TransactionHolder>>();

        private ConcurrentDictionary<Hash, TransactionHolder> _expired =
            new ConcurrentDictionary<Hash, TransactionHolder>();

        private ConcurrentDictionary<ulong, ConcurrentBag<TransactionHolder>> _expiredByRefBlockNumber =
            new ConcurrentDictionary<ulong, ConcurrentBag<TransactionHolder>>();

        public Func<ulong> CurrentHeightGetter;

        private Hash _chainId;

        private ulong _curHeight;

        public ulong CurHeight
        {
            get
            {
                if (_curHeight == 0)
                {
                    _curHeight = CurrentHeightGetter();
                }

                return _curHeight;
            }
        }

        public TxHub(ITransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
        }

        public void AddNewTransaction(Transaction transaction)
        {
//            if (transaction.RefBlockNumber < CurHeight - Globals.ReferenceBlockValidPeriod)
//            {
//                throw new Exception("Reference block is too old.");
//            }

            var holder = new TransactionHolder(transaction);
            if (!_allTxns.TryAdd(transaction.GetHash(), holder))
            {
                var txn = _transactionManager.GetTransaction(holder.TxId).Result; 
                if (txn == null || txn.Equals(new Transaction()))
                {
                    throw new Exception("Transaction already exists.");
                }

            }
        }

        public ITransactionHolderView GetTxHolderView(Hash txHash)
        {
            if (_allTxns.TryGetValue(txHash, out var tx))
            {
                return tx;
            }

            return null;
        }

        public Transaction GetTxForValidation()
        {
            foreach (var kv in _waiting)
            {
                if (kv.Value.ToValidating())
                {
                    return kv.Value.Transaction;
                }
            }

            return null;
        }

        public List<Transaction> GetTxsForExecution(int count)
        {
            int c = 0;
            var txhs = new List<TransactionHolder>();
            foreach (var kv in _validated)
            {
                if (count > 0 && c > count)
                {
                    break;
                }

                if (!kv.Value.ToExecuting()) continue;
                txhs.Add(kv.Value);
                c++;
            }

            foreach (var holder in txhs)
            {
                _executing.TryAdd(holder.TxId, holder);
                _validated.TryRemove(holder.TxId, out _);
            }

            return txhs.Select(x => x.Transaction).ToList();
        }

        public int ValidatedCount
        {
            get => _validated.Count;
        }

        public void ValidatingTx(Hash txHash)
        {
            var holder = GetTransactionHolder(txHash);

            if (holder.ToValidating())
            {
                _validating.TryAdd(txHash, holder);
                _waiting.TryRemove(txHash, out _);
            }
        }

        public void ValidatedTx(Hash txHash)
        {
            var holder = GetTransactionHolder(txHash);

            if (holder.ToValidated())
            {
                _validated.TryAdd(txHash, holder);
                _validating.TryRemove(txHash, out _);
            }
        }

        public void InvalidatedTx(Hash txHash)
        {
            var holder = GetTransactionHolder(txHash);

            if (holder.ToInvalid())
            {
                _invalid.TryAdd(txHash, holder);
                _validating.TryRemove(txHash, out _);
            }
        }

//        public void ExecutingTx(Hash txHash)
//        {
//            var holder = GetTransactionHolder(txHash);
//
//            if (holder.ToExecuting())
//            {
//                _executing.TryAdd(txHash, holder);
//                _validated.TryRemove(txHash, out _);
//            }
//        }

        public void RevertExecutingTx(Hash txHash)
        {
            var holder = GetTransactionHolder(txHash);

            if (holder.RevertExecuting())
            {
                _validated.TryAdd(txHash, holder);
                _executing.TryRemove(txHash, out _);
            }
        }

        public void ExecutedTx(Hash txHash)
        {
            var holder = GetTransactionHolder(txHash);

            if (holder.ToExecuted())
            {
                _executed.TryAdd(txHash, holder);
                var d = _executedByBlock.GetOrAdd(CurHeight + 1, new ConcurrentBag<TransactionHolder>());
                d.Add(holder);
                _executing.TryRemove(txHash, out _);
            }
        }


        public void OnNewBlockHeader(BlockHeader blockHeader)
        {
            if (blockHeader.Index != (CurHeight + 1) && CurHeight != 0)
            {
                throw new Exception("Invalid block index.");
            }

            _curHeight = blockHeader.Index;
            
            var expiredBns = new List<ulong>();
            if (blockHeader.Index > GlobalConfig.ReferenceBlockValidPeriod)
            {
                foreach (var kv in _validatedByRefBlockNumber)
                {
                    if (kv.Key < blockHeader.Index - GlobalConfig.ReferenceBlockValidPeriod)
                    {
                        expiredBns.Add(kv.Key);
                    }
                }
            }

            foreach (var bn in expiredBns)
            {
                if (_validatedByRefBlockNumber.TryRemove(bn, out var holders))
                {
                    foreach (var holder in holders)
                    {
                        if (holder.ToExpired())
                        {
                            throw new Exception("Chaning transaction to expired wrong.");
                        }

                        _expired.TryAdd(holder.TxId, holder);
                        _validated.TryRemove(holder.TxId, out _);
                        _allTxns.TryRemove(holder.TxId, out _);
                    }
                }
            }

            // Temporarily remove executed old transactions
            var toRemoveBns = new List<ulong>();
            var keepNBlocks = (ulong) 4;
            if (blockHeader.Index > keepNBlocks)
            {
                foreach (var kv in _executedByBlock)
                {
                    if (kv.Key < blockHeader.Index - keepNBlocks)
                    {
                        toRemoveBns.Add(kv.Key);
                    }
                }
            }

            foreach (var bn in toRemoveBns)
            {
                if (_executedByBlock.TryRemove(bn, out var holders))
                {
                    foreach (var holder in holders)
                    {
                        _executed.TryRemove(holder.TxId, out _);
                        _allTxns.TryRemove(holder.TxId, out _);
                    }
                }
            }
        }

        public void OnSwitchedFork(BlockHeader newBlockHeader)
        {
            // Revalidate all validated transactions
            foreach (var kv in _validated)
            {
                kv.Value.NeedRevalidating();
                _waiting.TryAdd(kv.Key, kv.Value);
            }

            _validated.Clear();
        }

//        public void GroupingTx(Hash txHash)
//        {
//            var holder = GetTransactionHolder(txHash);
//
//            if (holder.ToGrouping())
//            {
//                _grouping.TryAdd(txHash, holder);
//            }
//        }
//
//        public void GroupedTx(Hash txHash)
//        {
//            var holder = GetTransactionHolder(txHash);
//
//            if (holder.ToGrouped())
//            {
//                _grouped.TryAdd(txHash, holder);
//                _grouping.TryRemove(txHash, out _);
//            }
//        }

        private TransactionHolder GetTransactionHolder(Hash txHash)
        {
            if (!_allTxns.TryGetValue(txHash, out var holder))
            {
                throw new Exception($"Cannot find transaction for hash {txHash}.");
            }

            return holder;
        }
    }
}