using System;
using System.Collections.Generic;
using AElf.Common;
using AElf.Kernel;

namespace AElf.Consensus
{
    public interface IConsensusObserver : IObserver<BlockMiningEventData>
    {
        IDisposable Subscribe(byte[] consensusCommand, int chainId, Hash preBlockHash, ulong preBlockHeight);
    }
}