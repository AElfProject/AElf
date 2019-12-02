using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Miner.Application;
using AElf.Types;
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

        public async Task<List<Transaction>> GenerateTransactionsAsync(Address @from, long preBlockHeight, Hash preBlockHash)
        {
            return await _consensusService.GenerateConsensusTransactionsAsync(new ChainContext
                {BlockHash = preBlockHash, BlockHeight = preBlockHeight});
        }
    }
}