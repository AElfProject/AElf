using System.Collections.Generic;

namespace AElf.Kernel.Miner.Application
{
    public class SystemTransactionGenerationService : ISystemTransactionGenerationService
    {
        private readonly IEnumerable<ISystemTransactionGenerator> _systemTransactionGenerators;

        public SystemTransactionGenerationService(IEnumerable<ISystemTransactionGenerator> systemTransactionGenerators)
        {
            _systemTransactionGenerators = systemTransactionGenerators;
        }

        public List<Transaction> GenerateSystemTransactions(Address from, long preBlockHeight, Hash preBlockHash)
        {
            var generatedTxns = new List<Transaction>();
            foreach (var generator in _systemTransactionGenerators)
            {
                generator.GenerateTransactions(@from, preBlockHeight, preBlockHash, ref generatedTxns);
            }

            return generatedTxns;
        }
    }
}