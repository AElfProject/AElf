using System;
using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.Consensus
{
    public interface IConsensusObserver : IObserver<int>
    {
        IDisposable Subscribe(byte[] consensusCommand);
    }
}