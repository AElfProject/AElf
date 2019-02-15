using System;
using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.Consensus
{
    public interface IConsensusObserver : IObserver<ConsensusPerformanceType>
    {
        IDisposable Subscribe(byte[] consensusCommand);
    }
}