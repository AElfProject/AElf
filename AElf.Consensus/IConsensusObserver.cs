using System;

namespace AElf.Consensus
{
    public interface IConsensusObserver : IObserver<ConsensusPerformanceType>
    {
        IDisposable Subscribe(byte[] consensusCommand);
    }
}