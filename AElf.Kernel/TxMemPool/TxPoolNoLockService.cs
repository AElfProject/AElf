using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Services;

namespace AElf.Kernel.TxMemPool
{
    public class TxPoolNoLockService 
    {
        /*private readonly ITxPool _txPool;
        private readonly IAccountContextService _accountContextService;
        private bool flag;

        public TxPoolNoLockService(ITxPool txPool, IAccountContextService accountContextService)
        {
            _txPool = txPool;
            _accountContextService = accountContextService;
        }
        
        public void AddTx(Transaction tx)
        {
            if (!flag) return;
            if (!_txPool.Nonces.ContainsKey(tx.From))
                _txPool.Nonces.Add(tx.From,
                    _accountContextService.GetAccountDataContext(tx.From, _txPool.ChainId).Result.IncrementId
                );
            _txPool.EnQueueTx(tx);
        }

        public void Remove(Hash txHash)
        {
            //_txPool.DiscardTx(txHash);
        }

        public Task RemoveTxWithWorstFee()
        {
            throw new System.NotImplementedException();
        }

        public List<ITransaction> GetReadyTxs(ulong limit)
        {
            _txPool.Enqueueable = false;
            return _txPool.ReadyTxs(limit);
        }

        public Task<bool> Promote()
        {
            throw new System.NotImplementedException();
        }

        public ulong GetPoolSize()
        {
            return _txPool.Size;
        }

        /*public ITransaction GetTx(Hash txHash)
        {
            return _txPool.GetTx(txHash);
        }#1#

        public void Clear()
        {
            _txPool.ClearAll();
        }

        public Task SavePool()
        {
            throw new System.NotImplementedException();
        }

        public ulong GetWaitingSize()
        {
            return _txPool.GetWaitingSize();
        }

        public ulong GetExecutableSize()
        {
            return _txPool.GetExecutableSize();
        }

        public Task<ulong> GetTmpSize()
        {
            throw new System.NotImplementedException();
        }

        /*public void ResetAndUpdate(List<TransactionResult> txResultList)
        {
            foreach (var res in txResultList)
            {
                var hash = _txPool.GetTx(res.TransactionId).From;
                var id = _txPool.Nonces[hash];
                
                // update account context
                _accountContextService.SetAccountContext(new AccountDataContext
                {
                    IncrementId = id,
                    Address = hash,
                    ChainId = _txPool.ChainId
                });
            }
            
            _txPool.Enqueueable = true;
        }#1#

        public void Start()
        {
            flag = true;
        }

        public void Stop()
        {
            flag = false;
        }*/
    }
}