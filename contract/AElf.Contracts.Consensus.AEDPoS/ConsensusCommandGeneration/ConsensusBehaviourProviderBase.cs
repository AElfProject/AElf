using System.Linq;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        /// <summary>
        /// First step of getting consensus command for any pubkey:
        /// to get expected consensus behaviour.
        /// </summary>
        private abstract class ConsensusBehaviourProviderBase
        {
            protected readonly Round CurrentRound;

            private readonly string _pubkey;
            private readonly int _maximumBlocksCount;
            private readonly Timestamp _currentBlockTime;

            private readonly bool _isTimeSlotPassed;
            private readonly MinerInRound _minerInRound;

            protected ConsensusBehaviourProviderBase(Round currentRound, string pubkey, int maximumBlocksCount,
                Timestamp currentBlockTime)
            {
                CurrentRound = currentRound;

                _pubkey = pubkey;
                _maximumBlocksCount = maximumBlocksCount;
                _currentBlockTime = currentBlockTime;

                _isTimeSlotPassed = CurrentRound.IsTimeSlotPassed(_pubkey, _currentBlockTime);
                _minerInRound = CurrentRound.RealTimeMinersInformation[_pubkey];
            }

            public AElfConsensusBehaviour GetConsensusBehaviour()
            {
                // The most simple situation: provided pubkey isn't a miner.
                // Already checked in GetConsensusCommand.
//                if (!CurrentRound.IsInMinerList(_pubkey))
//                {
//                    return AElfConsensusBehaviour.Nothing;
//                }

                // If out value is null, it means provided pubkey hasn't mine any block during current round period.
                if (_minerInRound.OutValue == null)
                {
                    var behaviour = HandleMinerInNewRound();

                    // It's possible HandleMinerInNewRound can't handle all the situations, if this method returns Nothing,
                    // just go ahead. Otherwise, return it's result.
                    if (behaviour != AElfConsensusBehaviour.Nothing)
                    {
                        return behaviour;
                    }
                }
                else if (!_isTimeSlotPassed
                ) // Provided pubkey mined blocks during current round, and current block time is still in his time slot.
                {
                    if (_minerInRound.ActualMiningTimes.Count < _maximumBlocksCount)
                    {
                        // Provided pubkey can keep producing tiny blocks.
                        return AElfConsensusBehaviour.TinyBlock;
                    }

                    var blocksBeforeCurrentRound =
                        _minerInRound.ActualMiningTimes.Count(t => t <= CurrentRound.GetRoundStartTime());

                    // If provided pubkey is the one who terminated previous round, he can mine
                    // (_maximumBlocksCount + blocksBeforeCurrentRound) blocks
                    // because he has two time slots recorded in current round.

                    if (CurrentRound.ExtraBlockProducerOfPreviousRound ==
                        _pubkey && // Provided pubkey terminated previous round
                        !CurrentRound.IsMinerListJustChanged && // & Current round isn't the first round of current term
                        _minerInRound.ActualMiningTimes.Count.Add(1) <
                        _maximumBlocksCount.Add(
                            blocksBeforeCurrentRound) // & Provided pubkey hasn't mine enough blocks for current round.
                    )
                    {
                        // Then provided pubkey can keep producing tiny blocks.
                        return AElfConsensusBehaviour.TinyBlock;
                    }
                }

                return GetConsensusBehaviourToTerminateCurrentRound();
            }

            /// <summary>
            /// If this miner come to a new round, normally, there are three possible behaviour:
            /// UPDATE_VALUE (most common)
            /// TINY_BLOCK (happens if this miner is mining blocks for extra block time slot of previous round)
            /// NEXT_ROUND (only happens in first round)
            /// </summary>
            /// <returns></returns>
            private AElfConsensusBehaviour HandleMinerInNewRound()
            {
                if (
                    // For first round, the expected mining time is incorrect (due to configuration),
                    CurrentRound.RoundNumber == 1 &&
                    // so we'd better prevent miners' ain't first order (meanwhile he isn't boot miner) from mining fork blocks
                    _minerInRound.Order != 1 &&
                    // by postpone their mining time
                    CurrentRound.FirstMiner().OutValue == null
                )
                {
                    return AElfConsensusBehaviour.NextRound;
                }

                if (
                    // If this miner is extra block producer of previous round,
                    CurrentRound.ExtraBlockProducerOfPreviousRound == _pubkey &&
                    // and currently the time is ahead of current round,
                    _currentBlockTime < CurrentRound.GetRoundStartTime() &&
                    // make this miner produce some tiny blocks.
                    _minerInRound.ActualMiningTimes.Count < _maximumBlocksCount
                )
                {
                    return AElfConsensusBehaviour.TinyBlock;
                }

                return !_isTimeSlotPassed ? AElfConsensusBehaviour.UpdateValue : AElfConsensusBehaviour.Nothing;
            }

            /// <summary>
            /// Main Chain & Side Chains are different (because side chains have no election mechanism thus no NEXT_TERM behaviour).
            /// </summary>
            /// <returns></returns>
            protected abstract AElfConsensusBehaviour GetConsensusBehaviourToTerminateCurrentRound();
        }
    }
}