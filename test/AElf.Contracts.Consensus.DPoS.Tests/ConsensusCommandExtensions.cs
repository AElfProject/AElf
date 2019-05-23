using System;
using System.Linq;
using Acs4;
using Google.Protobuf;

namespace AElf.Contracts.Consensus.DPoS
{
    public static class ConsensusCommandExtensions
    {
        /// <summary>
        /// DPoS Behaviour is changeable in this method.
        /// It's the situation this miner should skip his time slot, more precisely.
        /// </summary>
        /// <param name="behaviour"></param>
        /// <param name="round"></param>
        /// <param name="publicKey"></param>
        /// <param name="dateTime"></param>
        /// <param name="isTimeSlotSkippable"></param>
        /// <returns></returns>
        internal static ConsensusCommand GetConsensusCommand(this DPoSBehaviour behaviour, Round round, string publicKey,
            DateTime dateTime, bool isTimeSlotSkippable)
        {
            var minerInRound = round.RealTimeMinersInformation[publicKey];
            var miningInterval = round.GetMiningInterval();
            var myOrder = round.RealTimeMinersInformation[minerInRound.PublicKey].Order;
            var expectedMiningTime = round.RealTimeMinersInformation[minerInRound.PublicKey].ExpectedMiningTime;

            int nextBlockMiningLeftMilliseconds;
            var hint = new DPoSHint {Behaviour = behaviour}.ToByteString();

            var previousMinerMissedHisTimeSlot = myOrder != 1 &&
                                                 round.RealTimeMinersInformation.Values
                                                     .First(m => m.Order == myOrder - 1).OutValue == null;
            var previousTwoMinersMissedTheirTimeSlot = myOrder > 2 &&
                                                       round.RealTimeMinersInformation.Values
                                                           .First(m => m.Order == myOrder - 1).OutValue == null &&
                                                       round.RealTimeMinersInformation.Values
                                                           .First(m => m.Order == myOrder - 2).OutValue == null;
            var skipTimeSlot = previousMinerMissedHisTimeSlot && !previousTwoMinersMissedTheirTimeSlot &&
                               isTimeSlotSkippable;

            var firstMinerOfCurrentRound =
                round.RealTimeMinersInformation.Values.FirstOrDefault(m => m.OutValue != null);

            switch (behaviour)
            {
                case DPoSBehaviour.UpdateValueWithoutPreviousInValue:
                    // Two reasons of `UpdateValueWithoutPreviousInValue` behaviour:
                    // 1. 1st round of 1st term.
                    // 2. Term changed in current round.
                    if (skipTimeSlot)
                    {
                        if (firstMinerOfCurrentRound != null)
                        {
                            var roundStartTimeInTheory = firstMinerOfCurrentRound.ActualMiningTime.ToDateTime()
                                .AddMilliseconds(-firstMinerOfCurrentRound.Order * miningInterval);
                            var minersCount = round.RealTimeMinersInformation.Count;
                            var extraBlockMiningTimeInTheory =
                                roundStartTimeInTheory.AddMilliseconds(minersCount * miningInterval);
                            nextBlockMiningLeftMilliseconds =
                                (int) (round.ArrangeAbnormalMiningTime(publicKey, extraBlockMiningTimeInTheory,
                                           miningInterval).ToDateTime() - dateTime).TotalMilliseconds;
                            // If someone produced block in current round before.

                            hint = new DPoSHint
                            {
                                Behaviour = DPoSBehaviour.NextRound
                            }.ToByteString();
                            break;
                        }

                        nextBlockMiningLeftMilliseconds = minerInRound.Order * miningInterval * 2 + miningInterval;
                        hint = new DPoSHint
                        {
                            Behaviour = DPoSBehaviour.NextRound
                        }.ToByteString();
                        break;
                    }

                    nextBlockMiningLeftMilliseconds = minerInRound.Order * miningInterval;
                    break;

                case DPoSBehaviour.UpdateValue:
                    // If miner of previous order didn't produce block, skip this time slot.
                    if (skipTimeSlot)
                    {
                        nextBlockMiningLeftMilliseconds = (int) (round.ArrangeAbnormalMiningTime(minerInRound.PublicKey,
                                                                     round.GetExtraBlockMiningTime(),
                                                                     round.GetMiningInterval()).ToDateTime() - dateTime)
                            .TotalMilliseconds;
                        hint = new DPoSHint
                        {
                            Behaviour = DPoSBehaviour.NextRound
                        }.ToByteString();
                        break;
                    }

                    nextBlockMiningLeftMilliseconds =
                        (int) (expectedMiningTime.ToDateTime() - dateTime).TotalMilliseconds;
                    break;
                case DPoSBehaviour.NextRound:
                    nextBlockMiningLeftMilliseconds = round.RoundNumber == 1
                        ? round.RealTimeMinersInformation.Count * miningInterval + myOrder * miningInterval
                        : (int) (round.ArrangeAbnormalMiningTime(minerInRound.PublicKey, dateTime).ToDateTime() -
                                 dateTime).TotalMilliseconds;
                    break;
                case DPoSBehaviour.NextTerm:
                    nextBlockMiningLeftMilliseconds =
                        (int) (round.ArrangeAbnormalMiningTime(minerInRound.PublicKey, dateTime).ToDateTime() -
                               dateTime).TotalMilliseconds;
                    break;
                default:
                    return new ConsensusCommand
                    {
                        ExpectedMiningTime = expectedMiningTime,
                        NextBlockMiningLeftMilliseconds = int.MaxValue,
                        LimitMillisecondsOfMiningBlock = int.MaxValue,
                        Hint = new DPoSHint
                        {
                            Behaviour = DPoSBehaviour.Nothing
                        }.ToByteString()
                    };
            }

            return new ConsensusCommand
            {
                ExpectedMiningTime = expectedMiningTime,
                NextBlockMiningLeftMilliseconds = nextBlockMiningLeftMilliseconds,
                LimitMillisecondsOfMiningBlock = miningInterval / minerInRound.PromisedTinyBlocks,
                Hint = hint
            };
        }
    }
}