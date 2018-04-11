using System.Collections.Generic;

namespace AElf.Kernel
{
    public class TxPool :ITxPool
    {
        private Dictionary<Hash, ITransaction> _executable = new Dictionary<Hash, ITransaction>();
        private Dictionary<Hash, ITransaction> _notExecutable = new Dictionary<Hash, ITransaction>();
        

        private IChainContext _context;
        private ITxPoolConfig _config;

        public TxPool(IChainContext context, ITxPoolConfig config)
        {
            _context = context;
            _config = config;
        }

        public bool AddTx(ITransaction tx)
        {
            throw new System.NotImplementedException();
        }

        public bool AddTxs(List<ITransaction> txs)
        {
            throw new System.NotImplementedException();
        }

        public void Remove(Hash txHash)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveTxAsWorstPrice()
        {
            throw new System.NotImplementedException();
        }

        public bool Validate(ITransaction tx)
        {
            throw new System.NotImplementedException();
        }

        public ulong PoolSize()
        {
            throw new System.NotImplementedException();
        }

        public bool GetTransaction(Hash txHash, out ITransaction tx)
        {
            throw new System.NotImplementedException();
        }
    }
}