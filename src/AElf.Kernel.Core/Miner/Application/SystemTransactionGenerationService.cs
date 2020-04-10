using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.Miner.Application
{
    public class SystemTransactionGenerationService : ISystemTransactionGenerationService
    {
        private readonly List<ISystemTransactionGenerator> _systemTransactionGenerators;

        public ILogger<SystemTransactionGenerationService> Logger { get; set; }

        public SystemTransactionGenerationService(IEnumerable<ISystemTransactionGenerator> systemTransactionGenerators)
        {
            _systemTransactionGenerators = systemTransactionGenerators.ToList();
            _systemTransactionGenerators.Sort((p1, p2) =>
                string.Compare(p2.SystemTransactionGeneratorName, p1.SystemTransactionGeneratorName, StringComparison.Ordinal));
        }

        public async Task<List<Transaction>> GenerateSystemTransactionsAsync(Address @from, long preBlockHeight,
            Hash preBlockHash)
        {
            var generatedTransactions = new List<Transaction>();
            foreach (var generator in _systemTransactionGenerators)
            {
                generatedTransactions.AddRange(
                    await generator.GenerateTransactionsAsync(@from, preBlockHeight, preBlockHash));
            }

            return generatedTransactions;
        }
    }
}