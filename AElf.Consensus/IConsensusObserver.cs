using System;
using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.Consensus
{
    public interface IConsensusObserver : IObserver<ConsensusPerformanceType>
    {
        List<Transaction> TransactionsForBroadcasting { get; set; }
        IDisposable Subscribe(byte[] consensusCommand);
    }
}