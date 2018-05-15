using System.Collections.Generic;

namespace AElf.Kernel.Concurrency
{
    public interface ITransactionParallelGroup
    {
        List<ITransaction> GetAccountTxList(Hash sender);
        void AddAccountTxList(KeyValuePair<Hash, List<ITransaction>> kvPair);
        List<Hash> GetSenderList();
        List<ITransaction> GetNextUnScheduledTxBatch();
        int GetSenderCount();
    }
}