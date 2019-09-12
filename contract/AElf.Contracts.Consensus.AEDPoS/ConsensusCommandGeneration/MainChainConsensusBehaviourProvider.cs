using System.Linq;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
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

            protected override AElfConsensusBehaviour GetConsensusBehaviourToTerminateCurrentRound()
            {
                // In first round, the blockchain start timestamp is incorrect.
                // We can return NextRound directly.
                if (_currentRound.RoundNumber == 1)
                {
                    return AElfConsensusBehaviour.NextRound;
                }

                var minimumMinersCount = _currentRound.GetMinimumMinersCount();
                var approvalsCount = _currentRound.RealTimeMinersInformation.Values
                    .Where(m => m.ActualMiningTimes.Any())
                    .Select(m => m.ActualMiningTimes.Last())
                    .Count(actualMiningTimestamp =>
                        IsTimeToChangeTerm(_blockchainStartTimestamp, actualMiningTimestamp, _currentRound.TermNumber,
                            _timeEachTerm));
                if (approvalsCount < minimumMinersCount)
                {
                    return AElfConsensusBehaviour.NextRound;
                }

                return AElfConsensusBehaviour.NextTerm;
            }

            /// <summary>
            /// If daysEachTerm == 7:
            /// 1, 1, 1 => 0 != 1 - 1 => false
            /// 1, 2, 1 => 0 != 1 - 1 => false
            /// 1, 8, 1 => 1 != 1 - 1 => true => term number will be 2
            /// 1, 9, 2 => 1 != 2 - 1 => false
            /// 1, 15, 2 => 2 != 2 - 1 => true => term number will be 3.
            /// </summary>
            /// <param name="blockchainStartTimestamp"></param>
            /// <param name="termNumber"></param>
            /// <param name="blockProducedTimestamp"></param>
            /// <param name="timeEachTerm"></param>
            /// <returns></returns>
            private bool IsTimeToChangeTerm(Timestamp blockchainStartTimestamp, Timestamp blockProducedTimestamp,
                long termNumber, long timeEachTerm)
            {
                return (blockProducedTimestamp - blockchainStartTimestamp).Seconds.Div(timeEachTerm) != termNumber - 1;
            }
        }
    }
}