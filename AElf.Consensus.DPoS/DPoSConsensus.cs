using System;
using AElf.Kernel;

namespace AElf.Consensus.DPoS
{
    // ReSharper disable InconsistentNaming
    public class DPoSConsensus : IConsensus
    {
        public bool ValidateConsensusInformation(ConsensusInformation consensusInformation)
        {
            throw new NotImplementedException();
        }

        public bool TryToGetNextMiningTime(out ulong distance)
        {
            throw new NotImplementedException();
        }

        public ConsensusInformation GenerateConsensusInformation()
        {
            throw new NotImplementedException();
        }
    }
}