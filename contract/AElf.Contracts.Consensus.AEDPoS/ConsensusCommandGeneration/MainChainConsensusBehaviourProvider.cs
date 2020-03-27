using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        private class MainChainConsensusBehaviourProvider : ConsensusBehaviourProviderBase
        {
            private readonly Timestamp _blockchainStartTimestamp;
            private readonly long _periodSeconds;

            public MainChainConsensusBehaviourProvider(Round currentRound, string pubkey, int maximumBlocksCount,
                Timestamp currentBlockTime, Timestamp blockchainStartTimestamp, long periodSeconds) : base(currentRound,
                pubkey, maximumBlocksCount, currentBlockTime)
            {
                _blockchainStartTimestamp = blockchainStartTimestamp;
                _periodSeconds = periodSeconds;
            }

            /// <summary>
            /// The blockchain start timestamp is incorrect during the first round,
            /// don't worry, we can return NextRound without hesitation.
            /// Besides, return only NextRound for single node running.
            /// </summary>
            /// <returns></returns>
            protected override AElfConsensusBehaviour GetConsensusBehaviourToTerminateCurrentRound() =>
                CurrentRound.RoundNumber == 1 || // Return NEXT_ROUND in first round.
                !CurrentRound.NeedToChangeTerm(_blockchainStartTimestamp,
                    CurrentRound.TermNumber, _periodSeconds) || 
                CurrentRound.RealTimeMinersInformation.Keys.Count == 1 // Return NEXT_ROUND for single node.
                    ? AElfConsensusBehaviour.NextRound
                    : AElfConsensusBehaviour.NextTerm;
        }
    }
}