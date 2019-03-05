using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Miner.Application;
using Volo.Abp.Threading;

namespace AElf.Kernel.Consensus.Application
{
    public class ConsensusTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly IConsensusService _consensusService;

        public ConsensusTransactionGenerator(IConsensusService consensusService)
        {
            _consensusService = consensusService;
        }
        
        public void GenerateTransactions(Address @from, long preBlockHeight, Hash previousBlockHash,
            ref List<Transaction> generatedTransactions)
        {
            var previousBlockPrefix = previousBlockHash.Value.Take(4).ToArray();
            generatedTransactions.AddRange(
                AsyncHelper.RunSync(() =>
                    _consensusService.GenerateConsensusTransactionsAsync(preBlockHeight, previousBlockPrefix)));
        }
    }
}