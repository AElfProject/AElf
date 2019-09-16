using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
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