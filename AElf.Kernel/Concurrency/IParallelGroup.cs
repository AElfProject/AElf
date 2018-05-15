using System.Collections.Generic;

namespace AElf.Kernel.Concurrency
{
    public interface IParallelGroup
    {
        List<ITransaction> GetAccountTxList(Hash sender);
        void AddAccountTxList(KeyValuePair<Hash, List<ITransaction>> kvPair);
        List<Hash> GetSenderList();
        int GetSenderCount();
        List<IBatch> Batches { get; }
    }
}