using System.Collections.Generic;
using AElf.Common;
using AElf.Execution.Execution;
using AElf.Kernel;
using AElf.Kernel.Txn;

namespace AElf.Consensus
{
    public class ConsensusTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly IExecutingService _executingService;

        public ConsensusTransactionGenerator(IExecutingService executingService)
        {
            _executingService = executingService;
        }
        
        public void GenerateTransactions(Address from, ulong preBlockHeight, ulong refBlockHeight, byte[] refBlockPrefix,
            ref List<Transaction> generatedTransactions)
        {
            throw new System.NotImplementedException();
        }
    }
}