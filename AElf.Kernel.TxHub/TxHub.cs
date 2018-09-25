using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AElf.Configuration;

namespace AElf.Kernel
{
    public class TxHub
    {
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

        private ConcurrentDictionary<Hash, TransactionHolder> _expired =
            new ConcurrentDictionary<Hash, TransactionHolder>();

        private ConcurrentDictionary<ulong, ConcurrentBag<TransactionHolder>> _expiredByRefBlockNumber =
            new ConcurrentDictionary<ulong, ConcurrentBag<TransactionHolder>>();
        
        private Hash _chainId;

        public ulong CurHeight { get; }

        public void AddNewTransaction(Transaction transaction)
        {
//            if (transaction.RefBlockNumber < CurHeight - Globals.ReferenceBlockValidPeriod)
//            {
//                throw new Exception("Reference block is too old.");
//            }

            if (!_allTxns.TryAdd(transaction.GetHash(), new TransactionHolder(transaction)))
            {
                throw new Exception("Transaction already exists.");
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
            var txs = new List<Transaction>();
            foreach (var kv in _validated)
            {
                if (count > 0 && c > count)
                {
                    return txs;
                }

                if (!kv.Value.ToExecuting()) continue;
                txs.Add(kv.Value.Transaction);
                c++;
            }

            return txs;
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
        
        public void ExecutingTx(Hash txHash)
        {
            var holder = GetTransactionHolder(txHash);

            if (holder.ToExecuting())
            {
                _executing.TryAdd(txHash, holder);
                _validated.TryRemove(txHash, out _);
            }
        }

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
                _executing.TryRemove(txHash, out _);
            }
        }

        
        public void OnNewBlockHeader(BlockHeader blockHeader)
        {
            var expiredBns = new List<ulong>();
            if (blockHeader.Index > Globals.ReferenceBlockValidPeriod)
            {
                foreach (var kv in _validatedByRefBlockNumber)
                {
                    if (kv.Key < blockHeader.Index - Globals.ReferenceBlockValidPeriod)
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