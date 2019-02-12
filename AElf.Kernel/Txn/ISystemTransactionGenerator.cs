using System.Collections.Generic;
using AElf.Common;

namespace AElf.Kernel.Txn
{
    public interface ISystemTransactionGenerator
    {
        void GenerateTransactions(Address from, ulong preBlockHeight, ulong refBlockHeight, byte[] refBlockPrefix,
            ref List<Transaction> generatedTransactions);
    }
}