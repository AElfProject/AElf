using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace AElf.Kernel.TxMemPool
{
    public class TxPool :ITxPool
    {
        private readonly Dictionary<Hash, List<Hash>> _executable =
            new Dictionary<Hash, List<Hash>>();
        private readonly Dictionary<Hash, Dictionary<ulong, Hash>> _waiting =
            new Dictionary<Hash, Dictionary<ulong, Hash>>();
        private readonly Dictionary<Hash, Transaction> _pool = new Dictionary<Hash, Transaction>();
        
        private readonly IAccountContextService _accountContextService;
        private readonly IChainContext _context;
        private readonly ITxPoolConfig _config;

        public TxPool(IChainContext context, ITxPoolConfig config, IAccountContextService accountContextService)
        {
            _context = context;
            _config = config;
            _accountContextService = accountContextService;
        }

        private HashSet<Hash> Tmp { get; } = new HashSet<Hash>();

        public List<Transaction> Ready
        {
            get
            {
                var list = new List<Transaction>();
                /*foreach (var p in _executable)
                {
                    var nonce = _accountContextService.GetAccountDataContext(p.Key, _context.ChainId).IncreasementId;
                    
                    foreach (var item in p.Value)
                    {
                        if (item.Key < nonce)
                        {
                            continue;
                        }
                        if(_pool.TryGetValue(item.Value, out var tx))
                            list.Add(tx);
                    }
                }*/
                foreach (var p in _executable)
                {
                    var nonce = _accountContextService.GetAccountDataContext(p.Key, _context.ChainId).IncreasementId;
                    foreach (var hash in p.Value)
                    {
                        if(_pool.TryGetValue(hash, out var tx) && tx.IncrementId >= nonce)
                            list.Add(tx);
                    }
                }
                return list;
            }
        }


        /// <inheritdoc />
        public ulong EntryThreshold => _config.EntryThreshold;

        /// <inheritdoc />
        public Fee MinimalFee => _config.FeeThreshold;

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
            // validate tx
            if (Contains(txHash)||!ValidateTx(tx))
            {
                return false;
            }
            _pool.Add(txHash, tx);
            Tmp.Add(txHash);
            return true;
        }

        public void QueueTxs()
        {
            foreach (var txHash in Tmp)
            {
                if (!Tmp.Contains(txHash)||!Contains(txHash)||ReplaceTx(txHash))
                    continue;
                if (!AddWaitingTx(txHash))
                    _pool.Remove(txHash);
            }
            Tmp.Clear();
        }

        /// <inheritdoc/>
        public bool DisgardTx(Hash txHash)
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

        /// <inheritdoc/>
        public ulong GetTmpSize()
        {
            return (ulong)Tmp.Count;
        }
        
        private bool ValidateTx(Transaction tx)
        {
            // fee check
            
            
            // size check
            /*if (GetTxSize(tx) > _config.TxLimitSize)
            {
                // TODO: log errors 
                return false;
            }*/
            
            // tx data validation
            /*if (tx.IncrementId < 0 || tx.MethodName == null || tx.From == null)
            {                
                // TODO: log errors 
                return false;
            }*/
            
            // TODO: signature validation
            
            
            // account address validation
           /* if (tx.From == null || !CheckAddress(tx.From) || !CheckAddress(tx.To))
            {
                // TODO: log errors 
                return false;
            }*/

            // tx overdue validation
            var acc = _accountContextService.GetAccountDataContext(tx.From, _context.ChainId);
            return acc.IncreasementId <= tx.IncrementId;

            // TODO : more validations
        }
        
        /// <summary>
        /// replace tx in pool with higher fee
        /// </summary>
        /// <param name="txHash"></param>
        /// <returns></returns>
        private bool ReplaceTx(Hash txHash)
        {
            if (!_pool.TryGetValue(txHash, out var tx)||!ValidateTx(tx))
                return false;
            var addr = tx.From;
            var nonce = _accountContextService.GetAccountDataContext(tx.From, _context.ChainId).IncreasementId;
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
            if (tx.IncrementId < _accountContextService.GetAccountDataContext(tx.From, _context.ChainId).IncreasementId)
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
            var nonce = _accountContextService.GetAccountDataContext(addr, _context.ChainId).IncreasementId;


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

        /// <summary>
        /// remove unvalid txs sent by account address addr, from executable list, like too old tx
        /// </summary>
        /// <param name="accountHash"></param>
        /// <returns></returns>
        private bool RemoveExecutedTxs(Hash accountHash)
        {
            var context = _accountContextService.GetAccountDataContext(accountHash, _context.ChainId);
            var nonce = context.IncreasementId;
            if (!_executable.TryGetValue(accountHash, out var list) || list.Count ==0)
                return false;

            // remove and return executed txs
            var hashesToRemove = list.GetRange(0, Math.Max(0, (int)(nonce - _pool[list[0]].IncrementId)));
            list.RemoveRange(0, Math.Max(0, (int)(nonce - _pool[list[0]].IncrementId)));
            
            // remove executed from pool
            foreach (var hash in hashesToRemove)
            {
                if (!_pool.Remove(hash))
                {
                    // TODO: log error
                }
            }
            return true;
        }
        
        /// <inheritdoc/>
        public void RemoveExecutedTxs()
        {
            foreach (var addr in _executable.Keys)
            {
                RemoveExecutedTxs(addr);
            }
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

        
        /// <summary>
        /// promote ready txs from waiting to exectuable
        /// </summary>
        /// <param name="addr"></param>
        private void Promote(Hash addr)
        {
            var waitingList = _waiting[addr];
            
            // discard too old txs
            // old txs
            /*var context = _accountContextService.GetAccountDataContext(addr, _context.ChainId);
            var nonce = context.IncreasementId;*/
            
            ulong w = 0;
            if (_executable.TryGetValue(addr, out var executableList))
            {
                w = _pool[executableList.Last()].IncrementId + 1;
            }
            
            var oldList = waitingList.Keys.Where(n => n < w).Select(n => waitingList[n]);
            
            // disgard
            foreach (var h in oldList)
            {
                RemoveFromWaiting(h);
            }
            
            // no tx left
            if (waitingList.Count == 0)
                return;
            var next = waitingList.Keys.Min();
            
            // no tx ready
            if (next != w)
                return;

            if (w == 0)
            {
                _executable[addr] = executableList = new List<Hash>();
            }
            
            do
            {
                var hash = waitingList[next];
                var tx = _pool[hash];
                // add to executable list
                executableList.Add(hash);
                // remove from waiting list
                waitingList.Remove(next);
               
            } while (waitingList.Count > 0 && waitingList.Keys.Contains(++next));


        }
      
        
        /// <summary>
        /// check validity of address
        /// </summary>
        /// <param name="accountHash"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private bool CheckAddress(Hash accountHash)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// return size of given tx
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private int GetTxSize(Transaction tx)
        {
            throw new System.NotImplementedException();
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