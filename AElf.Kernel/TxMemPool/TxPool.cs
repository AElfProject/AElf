using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace AElf.Kernel.TxMemPool
{
    public class TxPool :ITxPool
    {
        private readonly ConcurrentDictionary<Hash, SortedDictionary<ulong, Hash>> _executable =
            new ConcurrentDictionary<Hash, SortedDictionary<ulong, Hash>>();
        private readonly ConcurrentDictionary<Hash, SortedDictionary<ulong, Hash>> _waiting =
            new ConcurrentDictionary<Hash, SortedDictionary<ulong, Hash>>();
        private readonly Dictionary<Hash, ITransaction> _pool = new Dictionary<Hash, ITransaction>();
        
        private readonly IAccountContextService _accountContextService;
        private readonly IChainContext _context;
        private readonly ITxPoolConfig _config;

        public TxPool(IChainContext context, ITxPoolConfig config, IAccountContextService accountContextService)
        {
            _context = context;
            _config = config;
            _accountContextService = accountContextService;
        }
        
        public bool GetTransaction(Hash txHash, out ITransaction tx)
        {
            return _pool.TryGetValue(txHash, out tx);
        }
        
        public bool AddTx(ITransaction tx)
        {
            // validate tx
            if (!ValidateTx(tx))
            {
                return false;
            }
            
            // 1' try to add to replace one tx in executable
            // 2' if 1' failed, add to wainting List
            // TODO: more processings like pool expired checking, price compared
            return ReplaceExecutableTx(tx) || AddWaitingTx(tx);
            
        }

        public bool DisgardTx(ITransaction tx)
        {
            if (RemoveFromExecutable(tx, out var unValidTxList))
            {
                // case 1: tx in executable list
                // add unvalid tx to waiting List
                foreach (var t in unValidTxList)
                {
                    AddWaitingTx(t);
                }
            }
            else
            {
                // case 2: tx in waiting list
                RemoveFromWaiting(tx);
            }

            return _pool.Remove(tx.GetHash());

        }
        
        public bool ValidateTx(ITransaction tx)
        {
            // fee check
            
            // size check
            if (GetTxSize(tx) > _config.TxLimitSize)
            {
                // TODO: log errors 
                return false;
            }
            
            // tx data validation
            if (tx.IncrementId < 0 || tx.MethodName == null || tx.From == null)
            {                
                // TODO: log errors 
                return false;
            }
            
            // TODO: signature validation
            
            
            // account address validation
            if (tx.From == null || !CheckAddress(tx.From) || !CheckAddress(tx.To))
            {
                // TODO: log errors 
                return false;
            }

            // tx overdue validation
            var acc = _accountContextService.GetAccountDataContext(tx.From, _context.ChainId);
            return acc.IncreasementId <= tx.IncrementId;

            // TODO : more validations
        }
        
        public int GetPoolSize()
        {
            return _pool.Count;
        }
        
        
        /// <summary>
        /// replace tx in executable list with higher fee
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        private bool ReplaceExecutableTx(ITransaction tx)
        {
            var addr = tx.From;

            if (!_executable.TryGetValue(addr, out var executableList) ||
                !executableList.Keys.Contains(tx.IncrementId)) return false;
            
            // tx with the same IncrementId in executable list
            // TODO: compare two tx's fee, choose higher one and disgard the lower 
            return true;
        }
        
        
        /// <summary>
        /// add tx to waiting list
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        private bool AddWaitingTx(ITransaction tx)
        {
            if (!_waiting.TryGetValue(tx.From, out var waitingList))
            {
                _waiting[tx.From] = new SortedDictionary<ulong, Hash>();
            }
            
            if (waitingList.Keys.Contains(tx.IncrementId))
            {
                // TODO: compare two tx's fee, choose higher one and disgard the lower 
            }

            return true;
        }
        
        
         /// <summary>
        /// remove tx from executable list
        /// </summary>
        /// <param name="tx"></param>
        /// <param name="unValidTxList">unvalid txs because removing this tx</param>
        /// <returns></returns>
        private bool RemoveFromExecutable(ITransaction tx, out List<ITransaction> unValidTxList)
        {
            // remove the tx 
            var addr = tx.From;
            unValidTxList = null;

            // fail if not exist
            if (!_executable.TryGetValue(addr, out var executableList) ||
                !executableList.Keys.Contains(tx.IncrementId))
                return false;
            
            // remove the tx 
            executableList.Remove(tx.IncrementId);
            
            // return unvalid tx because removing 
            unValidTxList = executableList.Values.Where(h => _pool[h].IncrementId > tx.IncrementId).ToList()
                .Select(h => _pool[h]).ToList();
            

            // remove unvalid tx from executable list
            foreach (var t in unValidTxList)
            {
                executableList.Remove(t.IncrementId);
            }
            
            // remove the entry if empty
            if (executableList.Count == 0)
                _executable.TryRemove(addr, out var list);
            
            // Update the account nonce if needed
            var context = _accountContextService.GetAccountDataContext(addr, _context.ChainId);
            context.IncreasementId = Math.Min(context.IncreasementId, tx.IncrementId);

            return true;
        }

        
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
            
            // remove the tx
            waitingList.Remove(tx.IncrementId);
            
            // remove the entry if empty
            if (waitingList.Count == 0)
                _waiting.TryRemove(addr, out var list);
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
                var waitingList = _waiting[addr];
                if (waitingList.Count == 0)
                    continue;
                
                // discard too old txs
                var context = _accountContextService.GetAccountDataContext(addr, _context.ChainId);
                var nonce = context.IncreasementId;
                var oldList = _waiting[addr].Keys.Where(n => n < nonce);
                foreach (var n in oldList)
                {
                    // TODO: log
                    waitingList.Remove(n);
                }

                // promote ready txs
                Promote(addr, nonce);

            }
        }

        
        /// <summary>
        /// promote ready txs
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="nonce"></param>
        private void Promote(Hash addr, ulong nonce)
        {
            var waitingList = _waiting[addr];
            
            // no tx left
            if (waitingList.Count == 0)
                return;
            
            var next = waitingList.First().Key;
            
            // no tx ready
            if (next > nonce)
                return;

            if (!_executable.TryGetValue(addr, out var executableList))
            {
                _executable[addr] = new SortedDictionary<ulong, Hash>();
            }
            
            do
            {
                // remove from waiting list
                waitingList.Remove(next);
                // add to executable list
                executableList[next]=waitingList.First().Value;
            } while (waitingList.Count > 0 && waitingList.First().Key == ++next);

            if (waitingList.Count == 0)
                _waiting.TryRemove(addr, out var list);

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
        private ulong GetTxSize(ITransaction tx)
        {
            throw new System.NotImplementedException();
        }
        

        /// <summary>
        /// return count of txs executable
        /// </summary>
        /// <returns></returns>
        private ulong GetExecutableSize()
        {
            return _executable.Values.Aggregate<SortedDictionary<ulong, Hash>, ulong>(0,
                (current, p) => current + (ulong) p.Count);
        }

        /// <summary>
        /// return count of txs waiting
        /// </summary>
        /// <returns></returns>
        private ulong GetWaitingSize()
        {
            return _waiting.Values.Aggregate<SortedDictionary<ulong, Hash>, ulong>(0,
                (current, p) => current + (ulong) p.Count);
        }
        
        
        
        
    }
    
    
   /* // Defines a comparer to create a sorted set
    // that is sorted by the file extensions.
    public class TxSortedOption : IComparer<ITransaction>
    {
        public int Compare(ITransaction t1, ITransaction t2)
        {
            return (int)(t1.IncrementId - t2.IncrementId);
        }
    }*/
    
}