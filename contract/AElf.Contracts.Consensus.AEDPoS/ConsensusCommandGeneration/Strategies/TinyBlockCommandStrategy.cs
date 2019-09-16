using System.Linq;
using Acs4;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        public class TinyBlockCommandStrategy : CommandStrategyBase
        {
            public TinyBlockCommandStrategy(Round currentRound, string pubkey, Timestamp currentBlockTime) : base(
                currentRound, pubkey, currentBlockTime)
            {
            }

            public override ConsensusCommand GetAEDPoSConsensusCommand()
            {
                if (CurrentRound.RoundNumber == 1 ||
                    CurrentRound.RoundNumber == 2 
                    && !MinerInRound.IsThisANewRoundForThisMiner())
                {
                    var firstBlockMiningTime = MinerInRound.ActualMiningTimes.First();
                    return new ConsensusCommand
                    {
                        Hint = new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.TinyBlock}.ToByteString(),
                        ArrangedMiningTime = CurrentBlockTime.AddMilliseconds(TinyBlockMinimumInterval),
                        MiningDueTime = firstBlockMiningTime.AddMilliseconds(MiningInterval),
                        // Reduce limit if time not enough
                        LimitMillisecondsOfMiningBlock = DefaultBlockMiningLimit
                    };
                }
                var producedTinyBlocks = MinerInRound.ProducedTinyBlocks;
                var currentRoundStartTime = CurrentRound.GetStartTime();
                var producedTinyBlocksForPreviousRound =
                    MinerInRound.ActualMiningTimes.Count(t => t < currentRoundStartTime);
                var miningInterval = CurrentRound.GetMiningInterval();
                var timeForEachBlock = miningInterval.Div(AEDPoSContractConstants.TotalTinySlots);
                var timeSlotStartTimestamp = CurrentRound.GetExpectedMiningTime(Pubkey);
                var arrangedMiningTime = CurrentBlockTime.AddMilliseconds(TinyBlockMinimumInterval);
                
                return new ConsensusCommand
                {
                    Hint = new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.TinyBlock}.ToByteString(),
                    ArrangedMiningTime = arrangedMiningTime,
                    MiningDueTime = timeSlotStartTimestamp.AddMilliseconds(MiningInterval),
                    // Reduce limit if time not enough
                    LimitMillisecondsOfMiningBlock = DefaultBlockMiningLimit
                };

                if (MinerInRound.IsThisANewRoundForThisMiner())
                {
                    // After publishing out value (producing normal block)
                    arrangedMiningTime = arrangedMiningTime.AddMilliseconds(
                        CurrentRound.ExtraBlockProducerOfPreviousRound != Pubkey
                            ? producedTinyBlocks.Mul(timeForEachBlock)
                            // Previous extra block producer can produce double tiny blocks at most.
                            : producedTinyBlocks.Sub(producedTinyBlocksForPreviousRound).Mul(timeForEachBlock));
                }
                else
                {
                    // After generating information of next round (producing extra block)
                    arrangedMiningTime = CurrentRound.GetStartTime().AddMilliseconds(-miningInterval)
                        .AddMilliseconds(TinyBlockMinimumInterval);
                }


                int nextBlockMiningLeftMilliseconds;

                if (CurrentRound.RoundNumber == 1 ||
                    CurrentRound.RoundNumber == 2 && !MinerInRound.IsThisANewRoundForThisMiner())
                {
                    nextBlockMiningLeftMilliseconds =
                        GetNextBlockMiningLeftMillisecondsForFirstRound(MinerInRound, miningInterval);
                }
                else
                {
                    TuneExpectedMiningTimeForTinyBlock(miningInterval,
                        CurrentRound.GetExpectedMiningTime(Pubkey),
                        ref arrangedMiningTime);

                    nextBlockMiningLeftMilliseconds = (int) (arrangedMiningTime - CurrentBlockTime).Milliseconds();
                }
            }

            /// <summary>
            /// Based on first actual mining time of provided miner.
            /// </summary>
            /// <param name="minerInRound"></param>
            /// <param name="miningInterval"></param>
            /// <returns></returns>
            private int GetNextBlockMiningLeftMillisecondsForFirstRound(MinerInRound minerInRound, int miningInterval)
            {
                var firstActualMiningTime = minerInRound.ActualMiningTimes.First();
                var timeForEachBlock = miningInterval.Div(AEDPoSContractConstants.TotalTinySlots);
                var expectedMiningTime =
                    firstActualMiningTime.AddMilliseconds(timeForEachBlock.Mul(minerInRound.ProducedTinyBlocks));
                TuneExpectedMiningTimeForTinyBlock(miningInterval, firstActualMiningTime, ref expectedMiningTime);
                return (int) (expectedMiningTime - CurrentBlockTime).Milliseconds();
            }

            /// <summary>
            /// Finally make current block time in the range of (expected_mining_time, expected_mining_time + time_for_each_block)
            /// </summary>
            /// <param name="miningInterval"></param>
            /// <param name="originExpectedMiningTime"></param>
            /// <param name="expectedMiningTime"></param>
            private void TuneExpectedMiningTimeForTinyBlock(int miningInterval, Timestamp originExpectedMiningTime,
                ref Timestamp expectedMiningTime)
            {
                var timeForEachBlock = miningInterval.Div(AEDPoSContractConstants.TotalTinySlots);
                var currentBlockTime = CurrentBlockTime;
                while (expectedMiningTime < currentBlockTime &&
                       expectedMiningTime < originExpectedMiningTime.AddMilliseconds(miningInterval))
                {
                    expectedMiningTime = expectedMiningTime.AddMilliseconds(timeForEachBlock);
                }
            }
        }
    }
}