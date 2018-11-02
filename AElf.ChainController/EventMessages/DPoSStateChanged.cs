using AElf.Kernel.Consensus;

namespace AElf.ChainController.EventMessages
{
    // ReSharper disable once InconsistentNaming
    public class DPoSStateChanged
    {
        public ConsensusBehavior ConsensusBehavior { get; }
        public bool IsMining { get; }

        public DPoSStateChanged(ConsensusBehavior consensusBehavior, bool isMining)
        {
            ConsensusBehavior = consensusBehavior;
            IsMining = isMining;
        }
    }
}