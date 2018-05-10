using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.Services;

namespace AElf.Kernel.TxMemPool
{
    public class TxPool : ITxPool
    {
        private readonly Dictionary<Hash, List<Hash>> _executable =
            new Dictionary<Hash, List<Hash>>();
        private readonly Dictionary<Hash, Dictionary<ulong, Hash>> _waiting =
            new Dictionary<Hash, Dictionary<ulong, Hash>>();
        private readonly Dictionary<Hash, Transaction> _pool = new Dictionary<Hash, Transaction>();
        
        private readonly IAccountContextService _accountContextService;
        private readonly ITxPoolConfig _config;

        public TxPool(ITxPoolConfig config, IAccountContextService accountContextService)
        {
            _config = config;
            _accountContextService = accountContextService;
        }

        private HashSet<Hash> Tmp { get; } = new HashSet<Hash>();

        /// <inheritdoc />
        public ulong EntryThreshold => _config.EntryThreshold;

        /// <inheritdoc />
        public Hash ChainId => _config.ChainId;

        /// <inheritdoc />
        public uint TxLimitSize => _config.TxLimitSize;
        
        /// <inheritdoc/>
        public ulong TmpSize => (ulong)Tmp.Count;

        /// <inheritdoc/>
        public ulong MinimalFee => _config.EntryThreshold;

        /// <inheritdoc />
        //public Fee MinimalFee => _config.FeeThreshold;

        /// <inheritdoc />
        public ulong Size => (ulong) _pool.Count;

        
        /// <inheritdoc/>
        public bool GetTx(Hash txHash, out Transaction tx)
        {
            return _pool.TryGetValue(txHash, out tx);
        }
        
        /// <inheritdoc/>
        public Transaction GetTx(Hash txHash)
        {
            return GetTx(txHash, out var tx) ? tx : null;
        }

        /// <inheritdoc/>
        public void ClearAll()
        {
            ClearWaiting();
            ClearExecutable();
            Tmp.Clear();
            _pool.Clear();
        }

        /// <inheritdoc/>
        public void ClearWaiting()
        {
            _waiting.Clear();
        }

        /// <inheritdoc/>
        public void ClearExecutable()
        {
            _executable.Clear();
        }

        /// <inheritdoc/>
        public bool Contains(Hash txHash)
        {
            return _pool.ContainsKey(txHash);
        }
        
        
        public void GetPoolStates(out ulong executable, out ulong waiting, out ulong tmp)
        {
            executable = GetExecutableSize();
            waiting = GetWaitingSize();
            tmp = (ulong)Tmp.Count;
        }

        /// <inheritdoc/>
        public bool AddTx(Transaction tx)
        {
            var txHash = tx.GetHash();
            
            // TODO: validate tx
            
            if (Contains(txHash) || GetNonce(tx.From) > tx.IncrementId)
                return false;
            
            _pool.Add(txHash, tx);
            Tmp.Add(txHash);
            
            return true;
        }

        /// <inheritdoc/>
        public List<Transaction> ReadyTxs()
        {
            var list = new List<Transaction>();
            foreach (var p in _executable)
            {
                var nonce = GetNonce(p.Key);
                foreach (var hash in p.Value)
                {
                    if(_pool.TryGetValue(hash, out var tx) && tx.IncrementId >= nonce)
                        list.Add(tx);
                }
            }
            return list;
        }
        
        /// <inheritdoc/>
        public void QueueTxs()
        {
            foreach (var txHash in Tmp)
            {
                if (!Tmp.Contains(txHash) || !Contains(txHash) || ReplaceTx(txHash))
                    continue;
                
                if (!AddWaitingTx(txHash))
                    _pool.Remove(txHash);
            }
            
            Tmp.Clear();
        }

        /// <inheritdoc/>
        public bool DiscardTx(Hash txHash)
        {
            if (!GetTx(txHash, out var tx))
            {
                return false;
            }
            
            if (RemoveFromExecutable(txHash, out var unValidTxList))
            {
                // case 1: tx in executable list
                // move unvalid txs to waiting List
                foreach (var hash in unValidTxList)
                {
                    AddWaitingTx(hash);
                }
            }
            else if(!RemoveFromWaiting(txHash))
            {
                Tmp.Remove(txHash);
            }

            return _pool.Remove(tx.GetHash());

        }

        /// <inheritdoc/>
        public ulong GetExecutableSize()
        {
            return _executable.Values.Aggregate<List<Hash>, ulong>(0,
                (current, p) => current + (ulong) p.Count);
        }

        /// <inheritdoc/>
        public ulong GetWaitingSize()
        {
            return _waiting.Values.Aggregate<Dictionary<ulong, Hash>, ulong>(0,
                (current, p) => current + (ulong) p.Count);
        }
        
        
        /// <summary>
        /// replace tx in pool with higher fee
        /// </summary>
        /// <param name="txHash"></param>
        /// <returns></returns>
        private bool ReplaceTx(Hash txHash)
        {
            // TODO: validate tx

            if (!_pool.TryGetValue(txHash, out var tx))
                return false;
            var addr = tx.From;
            var nonce = GetNonce(addr);
            if (!_executable.TryGetValue(addr, out var executableList) || executableList.Count == 0 
                || tx.IncrementId < nonce || (int)(tx.IncrementId - nonce) >= executableList.Count)
                return false;
            
            // TODO: compare two tx's fee, choose higher one and disgard the lower
            /*var transaction = _pool[executableList[(int) (tx.IncrementId - nonce)]];
            if (tx.Fee < transaction.Fee)
            {
                
            }*/
            return false;
        }

        /// <summary>
        /// add tx to waiting list
        /// </summary>
        /// <param name="txHash"></param>
        /// <returns></returns>
        private bool AddWaitingTx(Hash txHash)
        {
            if (!_pool.TryGetValue(txHash, out var tx))
            {
                return false;
            }
            // disgard the tx if too old
            if (tx.IncrementId < GetNonce(tx.From))
                return false;
            
            var addr = tx.From;
            // disgard it if already pushed to exectuable list
            if (_executable.TryGetValue(addr, out var executableList) && executableList.Count > 0 &&
                _pool[executableList.Last()].IncrementId >= tx.IncrementId)
                return false;
            
            if (!_waiting.TryGetValue(tx.From, out var waitingList))
            {
                waitingList = _waiting[tx.From] = new Dictionary<ulong, Hash>();
            }

            if (waitingList.ContainsKey(tx.IncrementId))
            {
                // TODO: compare two tx's fee, choose higher one and disgard the lower 
            }
            else
            {
                // add to waiting list
                _waiting[tx.From].Add(tx.IncrementId, tx.GetHash());
            }
            
            return true;
        }
        
        
        /// <summary>
        /// remove tx from executable list
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="unValidTxList">invalid txs because removing this tx</param>
        /// <returns></returns>
        private bool RemoveFromExecutable(Hash hash, out IEnumerable<Hash> unValidTxList)
        {
            unValidTxList = null;

            if (!Contains(hash))
            {
                return false;
            }
            var tx = _pool[hash];
            // remove the tx 
            var addr = tx.From;
            var nonce = GetNonce(addr);


            // fail if not exist
            if (!_executable.TryGetValue(addr, out var executableList) ||
                executableList.Count <= (int)(tx.IncrementId - nonce) || 
                !executableList[(int)(tx.IncrementId - nonce)].Equals(tx.GetHash())) 
                return false;
            
            // return unvalid tx because removing 
            unValidTxList = executableList.GetRange((int) (tx.IncrementId - nonce + 1),
                executableList.Count - (int) (tx.IncrementId - nonce + 1));
            // remove
            executableList.RemoveRange((int) (tx.IncrementId - nonce),
                executableList.Count - (int) (tx.IncrementId - nonce));
            
            // remove the entry if empty
            if (executableList.Count == 0)
                _executable.Remove(addr);
            
            // Update the account nonce if needed
            /*var context = _accountContextService.GetAccountDataContext(addr, _context.ChainId);
            context.IncreasementId = Math.Min(context.IncreasementId, tx.IncrementId);
            */
            return true;
        }

        /// <inheritdoc/>
        public List<Transaction> RemoveExecutedTxs()
        {
            var res = new List<Transaction>();
            foreach (var addr in _executable.Keys)
            {
                var list = RemoveExecutedTxs(addr);
                if(list != null)
                    res.Concat(list);
            }
            return res;
        }
        
        
        /// <summary>
        /// remove unvalid txs sent by account address addr, from executable list, like too old tx
        /// </summary>
        /// <param name="accountHash"></param>
        /// <returns></returns>
        private List<Transaction> RemoveExecutedTxs(Hash accountHash)
        {
            var nonce = GetNonce(accountHash);
            if (!_executable.TryGetValue(accountHash, out var list) || list.Count ==0)
                return null;

            // remove and return executed txs
            var hashesToRemove = list.GetRange(0, Math.Max(0, (int)(nonce - _pool[list[0]].IncrementId)));
            list.RemoveRange(0, Math.Max(0, (int)(nonce - _pool[list[0]].IncrementId)));
            var res = new List<Transaction>();
            // remove executed from pool
            foreach (var hash in hashesToRemove)
            {
                if (Contains(hash))
                {
                    res.Add(_pool[hash]);
                    _pool.Remove(hash);
                }
                else
                {
                    // Todo : Log errors
                }
                
            }
            return res;
        }
        
        
        /// <summary>
        /// remove tx from waiting list
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        private bool RemoveFromWaiting(Hash hash)
        {
            var tx = _pool[hash];
            var addr = tx.From;
            if (!_waiting.TryGetValue(addr, out var waitingList) ||
                !waitingList.Keys.Contains(tx.IncrementId)) return false;
            
            // remove the tx from waiting list
            waitingList.Remove(tx.IncrementId);
            
            // remove from pool
            //_pool.Remove(tx.GetHash());
            
            // remove the entry if empty
            if (waitingList.Count == 0)
                _waiting.Remove(addr);
            return true;
        }
        
        
        /// <summary>
        /// promote txs from waiting to executable list
        /// </summary>
        /// <param name="addrs"></param>
        public void Promote(List<Hash> addrs = null)
        {
            if (addrs == null)
            {
                addrs = _waiting.Keys.ToList();
            }

            foreach (var addr in addrs)
            {
                Promote(addr);
            }
        }

        private ulong? GetNextPromotableTxId(Hash addr)
        {
            if (_waiting == null)
                return null;

            Dictionary<ulong, Hash> waitingDict = null;
            if (!_waiting.TryGetValue(addr, out var waitingList))
            {
                return null;
            }
            
            ulong w = 0;
            
            if (_executable.TryGetValue(addr, out var executableList))
            {
                w = _pool[executableList.Last()].IncrementId + 1;
            }
            
            // no tx left
            if (waitingList.Count <= 0)
                return null;

            ulong next = waitingList.Keys.Min();
            
            if (next != w)
                return null;
            
            if(executableList == null)
                _executable[addr] = new List<Hash>();
            
            return next;
        }

        /// <summary>
        /// remove tx before nonce w
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="w"></param>
        private void DiscardOldTransaction(Hash addr, ulong w)
        {
            var waitingList = _waiting[addr];
            
            var oldList = waitingList.Keys.Where(n => n < w).Select(n => waitingList[n]);
            
            // discard
            foreach (var h in oldList)
            {
                RemoveFromWaiting(h);
            }
        }

        /// <summary>
        /// promote ready txs from waiting to exectuable
        /// </summary>
        /// <param name="addr">From account addr</param>
        private void Promote(Hash addr)
        {
            ulong? next = GetNextPromotableTxId(addr);

            if (!next.HasValue)
                return;

            DiscardOldTransaction(addr, next.Value);

            if (!_executable.TryGetValue(addr, out var executableList) || !_waiting.TryGetValue(addr, out var waitingList))
            {
                return;
            }

            ulong incr = next.Value;
            
            do
            {
                var hash = waitingList[incr];
                
                // add to executable list
                executableList.Add(hash);
                // remove from waiting list
                waitingList.Remove(incr);

            } while (waitingList.Count > 0 && waitingList.Keys.Contains(++incr));

        }

        /// <summary>
        /// return incrementId of account
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        private ulong GetNonce(Hash addr)
        {
            return _accountContextService.GetAccountDataContext(addr, ChainId).IncreasementId;
        }
      
    }
    
    
   /* // Defines a comparer to create a sorted set
    // that is sorted by the file extensions.
    public class TxSortedOption : IComparer<Transaction>
    {
        public int Compare(Transaction t1, Transaction t2)
        {
            return (int)(t1.IncrementId - t2.IncrementId);
        }
    }*/
    
}