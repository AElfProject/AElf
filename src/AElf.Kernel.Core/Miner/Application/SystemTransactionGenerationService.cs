using System.Collections.Generic;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.Miner.Application
{
    public class SystemTransactionGenerationService : ISystemTransactionGenerationService
    {
        private readonly IEnumerable<ISystemTransactionGenerator> _systemTransactionGenerators;

        public ILogger<SystemTransactionGenerationService> Logger { get; set; }

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