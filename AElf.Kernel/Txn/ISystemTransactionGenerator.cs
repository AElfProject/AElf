using System.Collections.Generic;
using AElf.Common;

namespace AElf.Kernel
{
    public interface ISystemTransactionGenerator
    {
        void GenerateTransactions(Address from, ulong preBlockHeight, ulong refBlockHeight, byte[] refBlockPrefix,
            int chainId, ref List<Transaction> generatedTransactions);
    }
}