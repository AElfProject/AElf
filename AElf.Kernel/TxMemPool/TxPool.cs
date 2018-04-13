using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace AElf.Kernel.TxMemPool
{
    public class TxPool :ITxPool
    {
        private readonly Dictionary<Hash, SortedDictionary<ulong, ITransaction>> _executable = new Dictionary<Hash, SortedDictionary<ulong, ITransaction>>();
        private readonly Dictionary<Hash, SortedDictionary<ulong, ITransaction>> _waiting = new Dictionary<Hash, SortedDictionary<ulong, ITransaction>>();

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
            if (!ValidateTx(tx))
            {
                return false;
            }
            
            // validations success
            var addr = tx.From;
            
            if (_executable.TryGetValue(addr, out var executableList) && executableList.Keys.Contains(tx.IncrementId))
            {
                // tx with the same IncrementId in executable list
                // TODO: compare two tx's price, choose higher one
                return true;
            }
            
            // add to wainting queue
            if (_waiting.TryGetValue(addr, out var waitingQueue))
            {
                if (waitingQueue.Keys.Contains(tx.IncrementId))
                {
                    // TODO: compare two tx's price, choose higher one
                }
            }
            else
            {
                _waiting[addr] = new SortedDictionary<ulong, ITransaction> {{tx.IncrementId, tx}};
            }
            
            // TODO: more processings like pool expired checking, price compared
            return true;
        }

        public void Remove(ITransaction tx)
        {
            
            if (RemoveFromExecutable(tx, out var unValidTxList))
            {
                // case 1: tx in executable list
                // add unvalid tx to waiting queue
                return;
            }
            // case 2: tx in waiting list
            RemoveFromWaiting(tx);
        }

        public bool RemoveFromExecutable(ITransaction tx, out List<ITransaction> unValidTxList)
        {
            // remove the tx 
            var addr = tx.From;
            unValidTxList = null;
            
            if (!_executable.TryGetValue(addr, out var executableList) ||
                !executableList.Keys.Contains(tx.IncrementId)) return false;
            
            // remove the tx and return unvalid tx because removing 
            executableList.Remove(tx.IncrementId);
            unValidTxList = executableList.Values.Where(t => t.IncrementId > tx.IncrementId).ToList();
            return true;
        }

        public bool RemoveFromWaiting(ITransaction tx)
        {
            var addr = tx.From;
            if (!_waiting.TryGetValue(addr, out var waitingList) ||
                !waitingList.Keys.Contains(tx.IncrementId)) return false;
            
            // remove the tx
            waitingList.Remove(tx.IncrementId);
            return true;
        }

        private int GetTxSize(ITransaction tx)
        {
            throw new System.NotImplementedException();
        }
        
        
        public bool ValidateTx(ITransaction tx)
        {
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
            throw new System.NotImplementedException();
        }

        public bool GetTransaction(Hash txHash, out ITransaction tx)
        {
            throw new System.NotImplementedException();
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