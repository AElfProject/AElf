using System.Collections.Generic;
using AElf.Common;

namespace AElf.Kernel.Txn
{
    public interface ISystemTransactionGenerationService
    {
        List<Transaction> GenerateSystemTransactions(Address from, ulong preBlockHeight, ulong refBlockNumber,
            byte[] refBlockPrefix);
    }
}