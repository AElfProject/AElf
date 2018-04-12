using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;

namespace AElf.Kernel.TxMemPool
{
    public class TxPool :ITxPool
    {
        private Dictionary<Hash, List<ITransaction>> _executable = new Dictionary<Hash, List<ITransaction>>();
        private Dictionary<Hash, List<ITransaction>> _waiting = new Dictionary<Hash, List<ITransaction>>();

        private IAccountContextService _accountContextService;
        

        private IChainContext _context;
        private ITxPoolConfig _config;

        public TxPool(IChainContext context, ITxPoolConfig config, IAccountContextService accountContextService)
        {
            _context = context;
            _config = config;
            _accountContextService = accountContextService;
        }
        
        public bool AddTx(ITransaction tx)
        {
            if (!Validate(tx, out var executable))
            {
                return false;
            }
            var addr = tx.From;
            if (executable)
            {
                if(_executable.TryGetValue(addr, out var executableList))
                {
                    executableList.Add(tx);
                }
                else
                {
                    _executable[addr] = new List<ITransaction>{tx};
                }
            }
            else
            {
                if (_waiting.TryGetValue(addr, out var notExecutableList))
                {
                    notExecutableList.Add(tx);
                }
                else
                {
                    _waiting[addr] = new List<ITransaction>{tx};
                }
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
            
        }

        public bool RemoveFromWaiting(Hash txHash)
        {
            
        }

        public int GetTxSize(ITransaction tx)
        {
            throw new System.NotImplementedException();
        }
        
        public bool Validate(ITransaction tx, out bool executable)
        {
            // size check
            if (GetTxSize(tx) > _config.TxLimitSize)
            {
                // TODO: log errors 
                return false;
            }
            

            // TODO : more validations
            return true;
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
}