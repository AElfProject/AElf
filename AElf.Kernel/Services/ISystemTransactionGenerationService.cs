using System.Collections.Generic;
using AElf.Common;

namespace AElf.Kernel.Services
{
    public interface ISystemTransactionGenerationService
    {
        List<Transaction> GenerateSystemTransactions(Address from, ulong preBlockHeight, ulong refBlockNumber,
            byte[] refBlockPrefix, int chainId);
    }
}