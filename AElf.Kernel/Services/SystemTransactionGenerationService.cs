using System.Collections.Generic;
using AElf.Common;

namespace AElf.Kernel.Services
{
    public class SystemTransactionGenerationService : ISystemTransactionGenerationService
    {
        private readonly IEnumerable<ISystemTransactionGenerator> _systemTransactionGenerators;
        public SystemTransactionGenerationService(IEnumerable<ISystemTransactionGenerator> systemTransactionGenerators)
        {
            _systemTransactionGenerators = systemTransactionGenerators;
        }
        public List<Transaction> GenerateSystemTransactions(Address from, ulong preBlockHeight, ulong refBlockNumber, 
            byte[] refBlockPrefix, int chainId)
        {
            var generatedTxns = new List<Transaction>();
            foreach (var generator in _systemTransactionGenerators)
            {
                generator.GenerateTransactions(from, preBlockHeight, refBlockNumber, refBlockPrefix, chainId, ref generatedTxns);
            }

            return generatedTxns;
        }
    }
}