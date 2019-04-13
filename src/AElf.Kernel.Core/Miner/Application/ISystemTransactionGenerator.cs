using System.Collections.Generic;

namespace AElf.Kernel.Miner.Application
{
    public interface ISystemTransactionGenerator
    {
        void GenerateTransactions(Address @from, long preBlockHeight, Hash preBlockHash,
            ref List<Transaction> generatedTransactions);
    }
}