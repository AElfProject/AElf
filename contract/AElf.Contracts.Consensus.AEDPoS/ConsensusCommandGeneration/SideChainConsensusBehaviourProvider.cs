using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        public class SideChainConsensusBehaviourProvider : ConsensusBehaviourProviderBase
        {
            public SideChainConsensusBehaviourProvider(Round currentRound, string pubkey, int maximumBlocksCount,
                Timestamp currentBlockTime) : base(currentRound, pubkey, maximumBlocksCount, currentBlockTime)
            {
            }

            protected override AElfConsensusBehaviour GetConsensusBehaviourToTerminateCurrentRound()
            {
                return AElfConsensusBehaviour.NextRound;
            }
        }
    }
}