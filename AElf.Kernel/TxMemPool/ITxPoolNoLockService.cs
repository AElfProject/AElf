using System.Collections.Generic;

namespace AElf.Kernel.TxMemPool
{
    public interface ITxPoolNoLockService
    {
        void AddTx(Transaction tx);
        void Remove(Hash txHash);
        List<ITransaction> GetReadyTxs(ulong limit);
        ulong GetPoolSize();
        ITransaction GetTx(Hash txHash);
        void Clear();
        void ResetAndUpdate(List<TransactionResult> txResultList);
        void Start();
        void Stop();
    }
}