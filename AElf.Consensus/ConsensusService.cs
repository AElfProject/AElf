using System;
using System.Collections.Concurrent;
using System.Linq;
using AElf.Common;
using AElf.Kernel;

namespace AElf.Consensus
{
    public class ConsensusService
    {
        private readonly IConsensusObserver _consensusObserver;
        
        private IDisposable _consensusObservables = null;

        public ConsensusService(IConsensusObserver consensusObserver)
        {
            _consensusObserver = consensusObserver;
        }
    }
}