using AElf.Kernel.Consensus;

namespace AElf.Node.EventMessages
{
    public class ConsensusStateChanged
    {
        public ConsensusBehavior ConsensusBehavior { get; }
        
        public ConsensusStateChanged(ConsensusBehavior consensusBehavior)
        {
            ConsensusBehavior = consensusBehavior;
        }
    }
}