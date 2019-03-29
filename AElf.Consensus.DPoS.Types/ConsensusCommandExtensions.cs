using System;
using System.Linq;
using AElf.Kernel;
using Google.Protobuf;

namespace AElf.Consensus.DPoS
{
    public static class ConsensusCommandExtensions
    {
        public static ConsensusCommand GetConsensusCommand(this DPoSBehaviour behaviour, Round round, string publicKey,
            DateTime dateTime)
        {
            var minerInRound = round.RealTimeMinersInformation[publicKey];
            var miningInterval = round.GetMiningInterval();
            var myOrder = round.RealTimeMinersInformation[minerInRound.PublicKey].Order;
            switch (behaviour)
            {
                case DPoSBehaviour.UpdateValueWithoutPreviousInValue:
                    // If miner of previous order didn't produce block, skip this time slot.
                    if (myOrder != 1 &&
                        round.RealTimeMinersInformation.Values.First(m => m.Order == myOrder - 1).OutValue == null)
                    {
                        return new ConsensusCommand
                        {
                            NextBlockMiningLeftMilliseconds = minerInRound.Order * miningInterval * 2 + miningInterval,
                            LimitMillisecondsOfMiningBlock = miningInterval / minerInRound.PromisedTinyBlocks,
                            Hint = new DPoSHint
                            {
                                Behaviour = DPoSBehaviour.NextRound
                            }.ToByteString()
                        };
                    }

                    return new ConsensusCommand
                    {
                        NextBlockMiningLeftMilliseconds = minerInRound.Order * miningInterval,
                        LimitMillisecondsOfMiningBlock = miningInterval / minerInRound.PromisedTinyBlocks,
                        Hint = new DPoSHint
                        {
                            Behaviour = behaviour
                        }.ToByteString()
                    };

                case DPoSBehaviour.UpdateValue:
                    // If miner of previous order didn't produce block, skip this time slot.
                    myOrder = round.RealTimeMinersInformation[minerInRound.PublicKey].Order;
                    if (myOrder != 1 &&
                        round.RealTimeMinersInformation.Values.First(m => m.Order == myOrder - 1).OutValue == null)
                    {
                        var fakeDateTime = round.GetExpectedEndTime().ToDateTime().AddMilliseconds(-miningInterval);
                        return new ConsensusCommand
                        {
                            NextBlockMiningLeftMilliseconds =
                                (int) (round.ArrangeAbnormalMiningTime(minerInRound.PublicKey, fakeDateTime,
                                           round.GetMiningInterval()).ToDateTime() - dateTime).TotalMilliseconds,
                            LimitMillisecondsOfMiningBlock = miningInterval / minerInRound.PromisedTinyBlocks,
                            Hint = new DPoSHint
                            {
                                Behaviour = DPoSBehaviour.NextRound
                            }.ToByteString()
                        };
                    }

                    var expectedMiningTime = round.GetExpectedMiningTime(minerInRound.PublicKey);
                    return new ConsensusCommand
                    {
                        NextBlockMiningLeftMilliseconds = (int) (expectedMiningTime.ToDateTime() - dateTime)
                            .TotalMilliseconds,
                        LimitMillisecondsOfMiningBlock = miningInterval / minerInRound.PromisedTinyBlocks,
                        Hint = new DPoSHint
                        {
                            Behaviour = behaviour
                        }.ToByteString()
                    };
                case DPoSBehaviour.NextRound:
                    if (round.RoundNumber == 1)
                    {
                        return new ConsensusCommand
                        {
                            NextBlockMiningLeftMilliseconds = round.RealTimeMinersInformation.Count * miningInterval,
                            LimitMillisecondsOfMiningBlock = miningInterval / minerInRound.PromisedTinyBlocks,
                            Hint = new DPoSHint
                            {
                                Behaviour = behaviour
                            }.ToByteString()
                        };
                    }
                    return new ConsensusCommand
                    {
                        NextBlockMiningLeftMilliseconds =
                            (int) (round.ArrangeAbnormalMiningTime(minerInRound.PublicKey, dateTime).ToDateTime() -
                                   dateTime).TotalMilliseconds,
                        LimitMillisecondsOfMiningBlock = miningInterval / minerInRound.PromisedTinyBlocks,
                        Hint = new DPoSHint
                        {
                            Behaviour = behaviour
                        }.ToByteString()
                    };
                case DPoSBehaviour.NextTerm:
                    return new ConsensusCommand
                    {
                        NextBlockMiningLeftMilliseconds =
                            (int) (round.ArrangeAbnormalMiningTime(minerInRound.PublicKey, dateTime).ToDateTime() -
                                   dateTime).TotalMilliseconds,
                        LimitMillisecondsOfMiningBlock = miningInterval / minerInRound.PromisedTinyBlocks,
                        Hint = new DPoSHint
                        {
                            Behaviour = behaviour
                        }.ToByteString()
                    };
                case DPoSBehaviour.ChainNotInitialized:
                    return new ConsensusCommand
                    {
                        NextBlockMiningLeftMilliseconds = int.MaxValue,
                        LimitMillisecondsOfMiningBlock = int.MaxValue,
                        Hint = new DPoSHint
                        {
                            Behaviour = behaviour
                        }.ToByteString()
                    };
                default:
                    return new ConsensusCommand
                    {
                        NextBlockMiningLeftMilliseconds = int.MaxValue,
                        LimitMillisecondsOfMiningBlock = int.MaxValue,
                        Hint = new DPoSHint
                        {
                            Behaviour = DPoSBehaviour.ChainNotInitialized
                        }.ToByteString()
                    };
            }
        }
    }
}