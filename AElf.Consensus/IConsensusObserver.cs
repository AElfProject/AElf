using System;

namespace AElf.Consensus
{
    public interface IConsensusObserver
    {
        IDisposable SubscribeInitialProcess();
        IDisposable SubscribeMiningProcess();
    }
}