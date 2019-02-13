using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Execution.Execution;
using AElf.Kernel;
using AElf.Kernel.Txn;

namespace AElf.Consensus
{
    public class ConsensusTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly IConsensusService _consensusService;

        public ConsensusTransactionGenerator(IConsensusService consensusService)
        {
            _consensusService = consensusService;
        }
        
        public void GenerateTransactions(Address from, ulong preBlockHeight, ulong refBlockHeight, byte[] refBlockPrefix,
            ref List<Transaction> generatedTransactions, int chainId)
        {
            generatedTransactions.AddRange(
                _consensusService.GenerateConsensusTransactions(chainId, from, refBlockHeight, refBlockPrefix));
        }
    }
}