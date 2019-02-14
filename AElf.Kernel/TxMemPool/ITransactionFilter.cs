using System.Collections.Generic;

namespace AElf.Kernel.TxMemPool
{
    public interface ITransactionFilter
    {
        void Execute(List<Transaction> txs);
    }
}