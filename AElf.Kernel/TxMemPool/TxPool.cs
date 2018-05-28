using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Services;

namespace AElf.Kernel.TxMemPool
{
    public class TxPool : ITxPool
    {
        private readonly Dictionary<Hash, List<ITransaction>> _executable =
            new Dictionary<Hash, List<ITransaction>>();
        private readonly Dictionary<Hash, Dictionary<ulong, ITransaction>> _waiting =
            new Dictionary<Hash, Dictionary<ulong, ITransaction>>();
        //private readonly Dictionary<Hash, ITransaction> _pool = new Dictionary<Hash, ITransaction>();
        
        private readonly ITxPoolConfig _config;
        

        public TxPool(ITxPoolConfig config)
        {
            _config = config;
        }

        //private HashSet<Hash> Tmp { get; } = new HashSet<Hash>();

        /// <inheritdoc />
        public ulong EntryThreshold => _config.EntryThreshold;

        public bool Enqueueable { get; set; } = true;

        /// <inheritdoc />
        public Hash ChainId => _config.ChainId;

        /// <inheritdoc />
        public uint TxLimitSize => _config.TxLimitSize;
        
        /// <inheritdoc/>
        //public ulong TmpSize => (ulong)Tmp.Count;

        /// <inheritdoc/>
        public ulong MinimalFee => _config.FeeThreshold;

        /// <inheritdoc />
        //public Fee MinimalFee => _config.FeeThreshold;

       
        /// <inheritdoc />
        public ConcurrentDictionary <Hash, ulong> Nonces { get; } = new ConcurrentDictionary<Hash, ulong>();


        /// <inheritdoc />
        public ulong Size => GetPoolSize();
        
        /// <inheritdoc/>
        public void ClearAll()
        {
            ClearWaiting();
            ClearExecutable();
            //Tmp.Clear();
            //_pool.Clear();
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

        
        /// <summary>
        /// return pool size of executable, waiting
        /// </summary>
        public ulong GetPoolSize()
        {
            return GetExecutableSize() + GetWaitingSize();
        }

        /*/// <inheritdoc/>
        public bool AddTx(ITransaction tx)
        {
            var txHash = tx.GetHash();
            
            // TODO: validate tx
            
            if (Contains(txHash) || GetNonce(tx.From) > tx.IncrementId)
                return false;
            
            _pool.Add(txHash, tx);
            Tmp.Add(txHash);
            
            return true;
        }*/

        /// <inheritdoc/>
        public List<ITransaction> ReadyTxs(ulong limit)
        {
            var res = new List<ITransaction>();
            foreach (var kv in _executable)
            {
                if ((ulong) res.Count >= limit)
                    break;
                var nonce = GetNonce(kv.Key);
                var r = 0;
                foreach (var tx in kv.Value)
                {
                    if (tx.IncrementId < nonce) continue;
                    r++;
                    res.Add(tx);
                    if ((ulong) res.Count >= limit)
                        break;
                }
                
                // update incrementId in account data context
                AddNonce(kv.Key, (ulong) r);
                
                //remove txs from executable list 
                kv.Value.RemoveRange(0, r);
            }
            return res;
        }

        /// <inheritdoc/>
        public void EnQueueTxs(HashSet<ITransaction> tmp)
        {
            foreach (var tx in tmp)
            {
                EnQueueTx(tx);
            }
        }

        public bool EnQueueTx(ITransaction tx)
        {
            if (!this.ValidateTx(tx))
                return false;
            
            //_pool[txHash] = tx;
            return AddWaitingTx(tx);
        }

        /// <inheritdoc/>
        public bool DiscardTx(ITransaction tx)
        {
            // executable
            if (RemoveFromExecutable(tx, out var unValidTxList))
            {
                // case 1: tx in executable list
                // move unvalid txs to waiting List
                foreach (var hash in unValidTxList)
                {
                    AddWaitingTx(hash);
                }
                return true;
            }
            // in waiting 
            return RemoveFromWaiting(tx);


        }

        /// <inheritdoc/>
        public ulong GetExecutableSize()
        {
            return _executable.Values.Aggregate<List<ITransaction>, ulong>(0,
                (current, p) => current + (ulong) p.Count);
        }

        public void GetPoolState(out ulong executable, out ulong waiting)
        {
            waiting = GetWaitingSize();
            executable = GetExecutableSize();
        }

        /// <inheritdoc/>
        public ulong GetWaitingSize()
        {
            return _waiting.Values.Aggregate<Dictionary<ulong, ITransaction>, ulong>(0,
                (current, p) => current + (ulong) p.Count);
        }


        /// <summary>
        /// replace tx in pool with higher fee
        /// </summary>
        /// <param name="tx"></param>
        /// <param name="oldTx"></param>
        /// <returns></returns>
        private bool ReplaceTx(ITransaction tx, ITransaction oldTx)
        {
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
        /// <param name="tx"></param>
        /// <returns></returns>
        private bool AddWaitingTx(ITransaction tx)
        {
            // disgard the tx if too old
            if (tx.IncrementId < GetNonce(tx.From))
                return false;
            
            var addr = tx.From;
            
            // disgard it if already pushed to exectuable list
            if (_executable.TryGetValue(addr, out var executableList) && executableList.Count > 0 &&
                executableList.Last().IncrementId >= tx.IncrementId)
                return false;
            
            if (!_waiting.TryGetValue(tx.From, out var waitingList))
            {
                waitingList = _waiting[tx.From] = new Dictionary<ulong, ITransaction>();
            }
            
            /*var nonce = GetNonce(addr);
            var alreadyExecutable = _executable.TryGetValue(addr, out var executableList) && executableList.Count != 0
                                    && tx.IncrementId >= nonce && (int) (tx.IncrementId - nonce) < executableList.Count;*/
            
            // disgard it if cannot replace the tx with sam id already in waiting list
            if (waitingList.TryGetValue(tx.IncrementId, out var oldTx))
                return ReplaceTx(tx, oldTx);
            
            // add to waiting list
            waitingList.Add(tx.IncrementId, tx);
            
            return true;
        }
        
        
        /// <summary>
        /// remove tx from executable list
        /// </summary>
        /// <param name="tx"></param>
        /// <param name="unValidTxList">invalid txs because removing this tx</param>
        /// <returns></returns>
        private bool RemoveFromExecutable(ITransaction tx, out IEnumerable<ITransaction> unValidTxList)
        {
            unValidTxList = null;
            // remove the tx 
            var addr = tx.From;
            var nonce = GetNonce(addr);

            // fail if not exist
            if (!_executable.TryGetValue(addr, out var executableList) ||
                executableList.Count <= (int)(tx.IncrementId - nonce) || 
                !executableList[(int)(tx.IncrementId - nonce)].Equals(tx)) 
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

        /*/// <inheritdoc/>
        public List<ITransaction> RemoveExecutedTxs()
        {
            var res = new List<ITransaction>();
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
        private List<ITransaction> RemoveExecutedTxs(Hash accountHash)
        {
            var nonce = GetNonce(accountHash);
            if (!_executable.TryGetValue(accountHash, out var list) || list.Count ==0)
                return null;

            // remove and return executed txs
            var hashesToRemove = list.GetRange(0, Math.Max(0, (int)(nonce - _pool[list[0]].IncrementId)));
            list.RemoveRange(0, Math.Max(0, (int)(nonce - _pool[list[0]].IncrementId)));
            var res = new List<ITransaction>();
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
        }*/
        
        
        /// <summary>
        /// remove tx from waiting list
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        private bool RemoveFromWaiting(ITransaction tx)
        {
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

        /// <inheritdoc/>
        //public ulong ReadyTxCount { get; private set; }

        private ulong? GetNextPromotableTxId(Hash addr)
        {
            if (_waiting == null)
                return null;

            if (!_waiting.TryGetValue(addr, out var waitingList))
            {
                return null;
            }
            
            ulong w = 0;
            
            if (_executable.TryGetValue(addr, out var executableList))
            {
                w = executableList.Last().IncrementId + 1;
            }
            
            // no tx left
            if (waitingList.Count <= 0)
                return null;

            ulong next = waitingList.Keys.Min();
            
            if (next != w)
                return null;
            
            if(executableList == null)
                _executable[addr] = new List<ITransaction>();
            
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
        /// promote ready tx from waiting to exectuable
        /// </summary>
        /// <param name="addr">From account addr</param>
        private void Promote(Hash addr)
        {
            ulong? next = GetNextPromotableTxId(addr);

            if (!next.HasValue)
                return ;

            DiscardOldTransaction(addr, next.Value);

            if (!_executable.TryGetValue(addr, out var executableList) || !_waiting.TryGetValue(addr, out var waitingList))
            {
                return ;
            }

            ulong incr = next.Value;
            
            do
            {
                var tx = waitingList[incr];
                
                // add to executable list
                executableList.Add(tx);
                // remove from waiting list
                waitingList.Remove(incr);
                
            } while (waitingList.Count > 0 && waitingList.Keys.Contains(++incr));

        }

        /*/// <summary>
        /// return incrementId of account
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        private ulong GetNonce(Hash addr)
        {
            return _accountContextService.GetAccountDataContext(addr, ChainId).IncrementId;
        }

        private void AddNonce(Hash addr, int toAdd)
        {    
            _accountContextService.GetAccountDataContext(addr, ChainId).IncrementId += (ulong) toAdd;
        }*/
        
        /// <summary>
        /// return incrementId of account
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        private ulong GetNonce(Hash addr)
        {
            if (Nonces.TryGetValue(addr, out var n))
            {
                return n;
            }
            throw new KeyNotFoundException("Not Found Account");
        }

        /// <summary>
        /// update nonce
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="increment"></param>
        /// <returns></returns>
        private void AddNonce(Hash addr, ulong increment)
        {
            var n = GetNonce(addr);
            Nonces[addr] = n + increment;
        }
      
    }
    
}