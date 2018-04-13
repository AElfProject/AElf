using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace AElf.Kernel.TxMemPool
{
    public class TxPool :ITxPool
    {
        private readonly Dictionary<Hash, SortedDictionary<ulong, ITransaction>> _executable =
            new Dictionary<Hash, SortedDictionary<ulong, ITransaction>>();

        private readonly Dictionary<Hash, SortedDictionary<ulong, ITransaction>> _waiting =
            new Dictionary<Hash, SortedDictionary<ulong, ITransaction>>();

        private readonly IAccountContextService _accountContextService;
        private readonly IChainContext _context;
        private readonly ITxPoolConfig _config;

        public TxPool(IChainContext context, ITxPoolConfig config, IAccountContextService accountContextService)
        {
            _context = context;
            _config = config;
            _accountContextService = accountContextService;
        }
        
        public bool AddTx(ITransaction tx)
        {
            // validate tx
            if (!ValidateTx(tx))
            {
                return false;
            }
            
            // 1. try to 
            // 2. if 1 failed, add to wainting List
            // TODO: more processings like pool expired checking, price compared
            return ReplaceExecutableTx(tx) || AddWaitingTx(tx);
            
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
                _waiting[tx.From] = new SortedDictionary<ulong, ITransaction>();
            }
            
            if (waitingList.Keys.Contains(tx.IncrementId))
            {
                // TODO: compare two tx's fee, choose higher one and disgard the lower 
            }

            return true;
        }
        
        
        public bool Remove(ITransaction tx)
        {
            // case 1: tx in executable list
            if (RemoveFromExecutable(tx, out var unValidTxList))
            {
                // add unvalid tx to waiting List
                foreach (var t in unValidTxList)
                {
                    AddWaitingTx(t);
                }
                return true;
            }
            
            // case 2: tx in waiting list
            return RemoveFromWaiting(tx);
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
                _executable[addr] = new SortedDictionary<ulong, ITransaction>();
            }
            
            do
            {
                // remove from waiting list
                waitingList.Remove(next);
                // add to executable list
                executableList[next]=waitingList.First().Value;
            } while (waitingList.Count > 0 && waitingList.First().Key == ++next);

            if (waitingList.Count == 0)
                _waiting.Remove(addr);

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
            
            if (!_executable.TryGetValue(addr, out var executableList) ||
                !executableList.Keys.Contains(tx.IncrementId)) return false;
            
            // remove the tx 
            executableList.Remove(tx.IncrementId);
            
            // return unvalid tx because removing 
            unValidTxList = executableList.Values.Where(t => t.IncrementId > tx.IncrementId).ToList();
            
            // remove unvalid tx from executable list
            foreach (var t in unValidTxList)
            {
                executableList.Remove(t.IncrementId);
            }
            
            // remove the entry if empty
            if (executableList.Count == 0)
                _executable.Remove(addr);
            
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
                _waiting.Remove(addr);
            return true;
        }

        private ulong GetTxSize(ITransaction tx)
        {
            throw new System.NotImplementedException();
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
            if (acc.IncreasementId > tx.IncrementId)
            {
                // TODO: log errors 
                return false;
            }

            // TODO : more validations
            return true;
        }

        private bool CheckAddress(Hash accountHash)
        {
            throw new NotImplementedException();
        }

        public ulong GetPoolSize()
        {
            throw new NotImplementedException();
        }

        public bool GetTransaction(Hash txHash, out ITransaction tx)
        {
            throw new NotImplementedException();
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