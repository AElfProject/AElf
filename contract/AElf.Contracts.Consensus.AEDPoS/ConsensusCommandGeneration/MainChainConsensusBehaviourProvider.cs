using System.Linq;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        public class MainChainConsensusBehaviourProvider : ConsensusBehaviourProviderBase
        {
            private readonly Timestamp _blockchainStartTimestamp;
            private readonly long _timeEachTerm;

            public MainChainConsensusBehaviourProvider(Round currentRound, string pubkey, int maximumBlocksCount,
                Timestamp currentBlockTime, Timestamp blockchainStartTimestamp, long timeEachTerm) : base(currentRound,
                pubkey, maximumBlocksCount, currentBlockTime)
            {
                _blockchainStartTimestamp = blockchainStartTimestamp;
                _timeEachTerm = timeEachTerm;
            }

            /// <summary>
            /// In the first round, the blockchain start timestamp is incorrect,
            /// thus we can return NextRound directly.
            /// </summary>
            /// <returns></returns>
            protected override AElfConsensusBehaviour GetConsensusBehaviourToTerminateCurrentRound() =>
                CurrentRound.RoundNumber == 1 || !CurrentRound.NeedToChangeTerm(_blockchainStartTimestamp,
                    CurrentRound.TermNumber, _timeEachTerm)
                    ? AElfConsensusBehaviour.NextRound
                    : AElfConsensusBehaviour.NextTerm;
        }
    }
}