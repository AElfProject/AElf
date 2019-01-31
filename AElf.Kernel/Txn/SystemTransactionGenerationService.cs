using System.Collections.Generic;
using AElf.Common;

namespace AElf.Kernel.Txn
{
    public class SystemTransactionGenerationService : ISystemTransactionGenerationService
    {
        private readonly List<ISystemTransactionGenerator> _systemTransactionGenerators;
        public SystemTransactionGenerationService(List<ISystemTransactionGenerator> systemTransactionGenerators)
        {
            _systemTransactionGenerators = systemTransactionGenerators;
        }
        public List<Transaction> GenerateSystemTransactions(Address from, ulong preBlockHeight, ulong refBlockNumber, byte[] refBlockPrefix)
        {
            var generatedTxns = new List<Transaction>();
            foreach (var generator in _systemTransactionGenerators)
            {
                generator.GenerateTransactions(from, preBlockHeight, refBlockNumber, refBlockPrefix, ref generatedTxns);
            }

            return generatedTxns;
        }
    }
}    