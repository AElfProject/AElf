using System;

namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusObserver : IObserver<int>
    {
        IDisposable Subscribe(byte[] consensusCommand);
    }
}