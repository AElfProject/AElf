using System;
using System.Collections.Concurrent;
using System.Linq;
using AElf.Common;
using AElf.Kernel;

namespace AElf.Consensus
{
    public class ConsensusService
    {
        private readonly IConsensus _consensus;
        private readonly IConsensusObserver _consensusObserver;
        
        private IDisposable _consensusObservables = null;
        private readonly ConcurrentDictionary<Hash, ConsensusInformation> _consensusInformations;

        public ConsensusService(IConsensus consensus, IConsensusObserver consensusObserver)
        {
            _consensus = consensus;
            _consensusObserver = consensusObserver;
        }

        private void HandleNewConsensusInformation(ConsensusInformation consensusInformation)
        {
            if (consensusInformation.PreviousBlockHash == Hash.Genesis && HasPreviousConsensus())
            {
                
            }
        }

        private ConsensusInformation GetPreviousConsensusInformation(Hash previousBlockHash)
        {
            return _consensusInformations.TryGetValue(previousBlockHash, out var consensusInformation)
                ? consensusInformation
                : null;
        }

        private bool HasPreviousConsensus()
        {
            return !_consensusInformations.Any();
        }
    }
}