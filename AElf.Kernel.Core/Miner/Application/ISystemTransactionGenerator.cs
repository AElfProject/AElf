using System.Collections.Generic;
using AElf.Common;

namespace AElf.Kernel.Miner.Application
{
    public interface ISystemTransactionGenerator
    {
        void GenerateTransactions(Address @from, ulong preBlockHeight, Hash preBlockHash,
            ref List<Transaction> generatedTransactions);
    }
}