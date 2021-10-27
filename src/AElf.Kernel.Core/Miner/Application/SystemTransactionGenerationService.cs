using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.Miner.Application
{
    public class SystemTransactionGenerationService : ISystemTransactionGenerationService
    {
        private readonly IEnumerable<ISystemTransactionGenerator> _systemTransactionGenerators;

        public ILogger<SystemTransactionGenerationService> Logger { get; set; }

        // TODO: A better strategy to control system transaction order.
        public SystemTransactionGenerationService(IEnumerable<ISystemTransactionGenerator> systemTransactionGenerators)
        {
            _systemTransactionGenerators = systemTransactionGenerators;
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