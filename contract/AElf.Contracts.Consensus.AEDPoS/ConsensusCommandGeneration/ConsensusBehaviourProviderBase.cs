using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        public abstract class ConsensusBehaviourProviderBase : IConsensusBehaviourProvider
        {
            protected readonly Round CurrentRound;
            protected readonly string Pubkey;
            protected readonly int MaximumBlocksCount;
            protected readonly Timestamp CurrentBlockTime;

            protected readonly bool IsTimeSlotPassed;
            protected readonly MinerInRound MinerInRound;

            protected ConsensusBehaviourProviderBase(Round currentRound, string pubkey, int maximumBlocksCount,
                Timestamp currentBlockTime)
            {
                CurrentRound = currentRound;
                Pubkey = pubkey;
                MaximumBlocksCount = maximumBlocksCount;
                CurrentBlockTime = currentBlockTime;

                IsTimeSlotPassed = CurrentRound.IsTimeSlotPassed(Pubkey, CurrentBlockTime);
                MinerInRound = CurrentRound.RealTimeMinersInformation[Pubkey];
            }

            public AElfConsensusBehaviour GetConsensusBehaviour()
            {
                if (!CurrentRound.IsInMinerList(Pubkey))
                {
                    return AElfConsensusBehaviour.Nothing;
                }

                if (!MinerInRound.IsThisANewRoundForThisMiner())
                {
                    var behaviour = HandleMinerInNewRound();
                    if (behaviour != AElfConsensusBehaviour.Nothing)
                    {
                        return behaviour;
                    }
                }
                else if (!IsTimeSlotPassed)
                {
                    if (MinerInRound.ProducedTinyBlocks < MaximumBlocksCount)
                    {
                        return AElfConsensusBehaviour.TinyBlock;
                    }

                    if (CurrentRound.ExtraBlockProducerOfPreviousRound == Pubkey &&
                        !CurrentRound.IsMinerListJustChanged &&
                        MinerInRound.ProducedTinyBlocks < MaximumBlocksCount.Mul(2))
                    {
                        if (CurrentRound.IsMinerListJustChanged &&
                            MinerInRound.ProducedTinyBlocks > MaximumBlocksCount.Add(1))
                        {
                            // Because NextTerm time slot only produces one block.
                            return AElfConsensusBehaviour.NextRound;
                        }

                        return AElfConsensusBehaviour.TinyBlock;
                    }
                }

                return GetConsensusBehaviourToTerminateCurrentRound();
            }

            /// <summary>
            /// If this miner come to a new round, normally, there are three possible behaviour:
            /// UpdateValue (most common)
            /// UpdateValueWithoutPreviousInValue (happens if current round is the first round of current term)
            /// TinyBlock (happens if this miner is mining blocks for extra block time slot of previous round)
            /// NextRound (only happens in first round)
            /// </summary>
            /// <returns></returns>
            private AElfConsensusBehaviour HandleMinerInNewRound()
            {
                if (CurrentRound.RoundNumber ==
                    1 && // For first round, the expected mining time is incorrect (due to configuration),
                    MinerInRound.Order !=
                    1 && // so we'd better prevent miners' ain't first order (meanwhile he isn't boot miner) from mining fork blocks
                    CurrentRound.FirstMiner().OutValue == null // by postpone their mining time
                )
                {
                    return AElfConsensusBehaviour.NextRound;
                }

                if (CurrentRound.ExtraBlockProducerOfPreviousRound ==
                    Pubkey && // If this miner is extra block producer of previous round,
                    CurrentBlockTime <
                    CurrentRound.GetStartTime() && // and currently the time is ahead of current round,
                    MinerInRound.ProducedTinyBlocks < MaximumBlocksCount // make this miner produce some tiny blocks.
                )
                {
                    return AElfConsensusBehaviour.TinyBlock;
                }

                if (CurrentRound.IsMinerListJustChanged)
                {
                    return AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue;
                }

                return !IsTimeSlotPassed ? AElfConsensusBehaviour.UpdateValue : AElfConsensusBehaviour.Nothing;
            }

            protected abstract AElfConsensusBehaviour GetConsensusBehaviourToTerminateCurrentRound();
        }
    }
}