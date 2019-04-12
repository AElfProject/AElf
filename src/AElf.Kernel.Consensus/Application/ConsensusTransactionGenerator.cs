using System.Collections.Generic;
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
        
        public void GenerateTransactions(Address from, long preBlockHeight, Hash previousBlockHash,
            ref List<Transaction> generatedTransactions)
        {
            generatedTransactions.AddRange(
                AsyncHelper.RunSync(() =>
                    _consensusService.GenerateConsensusTransactionsAsync(new ChainContext
                        {BlockHash = previousBlockHash, BlockHeight = preBlockHeight})));
        }
    }
}