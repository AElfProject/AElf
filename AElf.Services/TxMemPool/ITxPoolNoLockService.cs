using System.Collections.Generic;
using AElf.Kernel.Types;
using AElf.Kernel;

namespace AElf.Services.TxMemPool
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