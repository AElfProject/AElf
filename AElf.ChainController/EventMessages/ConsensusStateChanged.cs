using AElf.Kernel.Consensus;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
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