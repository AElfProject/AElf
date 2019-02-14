using System.Collections.Generic;

namespace AElf.Kernel
{
    public interface ITransactionFilter
    {
        void Execute(List<Transaction> txs);
    }
}