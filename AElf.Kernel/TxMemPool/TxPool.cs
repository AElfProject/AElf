using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace AElf.Kernel.TxMemPool
{
    public class TxPool :ITxPool
    {
        private readonly Dictionary<Hash, HashSet<ITransaction>> _executable = new Dictionary<Hash, HashSet<ITransaction>>();
        private readonly Dictionary<Hash, HashSet<ITransaction>> _waiting = new Dictionary<Hash, HashSet<ITransaction>>();

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
            
            if (_executable[addr].Contains(tx))
            {
                // TODO: compare two tx's price, choose higher one
                return true;
            }
            
            
            if (_waiting.TryGetValue(addr, out var notExecutableList))
            {
                notExecutableList.Add(tx);
            }
            else
            {
                _waiting[addr] = new HashSet<ITransaction> {tx};
            }
            
            
            // TODO: more processing like pool expired checking, price compared
            return true;
        }

        public void Remove(Hash txHash)
        {
            throw new System.NotImplementedException();
        }

        public bool RemoveFromExecutable(Hash txHash)
        {
            throw new System.NotImplementedException();
        }

        public bool RemoveFromWaiting(Hash txHash)
        {
            throw new System.NotImplementedException();
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