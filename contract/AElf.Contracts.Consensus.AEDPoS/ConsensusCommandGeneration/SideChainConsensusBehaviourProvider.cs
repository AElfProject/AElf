using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        private class SideChainConsensusBehaviourProvider : ConsensusBehaviourProviderBase
        {
            public SideChainConsensusBehaviourProvider(Round currentRound, string pubkey, int maximumBlocksCount,
                Timestamp currentBlockTime) : base(currentRound, pubkey, maximumBlocksCount, currentBlockTime)
            {
            }

            /// <summary>
            /// Simply return NEXT_ROUND for side chain.
            /// </summary>
            /// <returns></returns>
            protected override AElfConsensusBehaviour GetConsensusBehaviourToTerminateCurrentRound() =>
                AElfConsensusBehaviour.NextRound;
        }
    }
}