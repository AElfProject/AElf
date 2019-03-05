using System.Collections.Generic;
using AElf.Common;

namespace AElf.Kernel.Miner.Application
{
    public interface ISystemTransactionGenerationService
    {
        List<Transaction> GenerateSystemTransactions(Address from, long preBlockHeight, Hash preBlockHash);
    }
}