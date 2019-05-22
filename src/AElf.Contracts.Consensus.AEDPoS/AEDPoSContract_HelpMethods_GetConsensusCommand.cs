using System;
using System.Linq;
using Acs4;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        /// <summary>
        /// AElf Consensus Behaviour is changeable in this method.
        /// It's the situation this miner should skip his time slot more precisely.
        /// </summary>
        /// <param name="behaviour"></param>
        /// <param name="currentRound"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        private ConsensusCommand GetConsensusCommand(AElfConsensusBehaviour behaviour, Round currentRound,
            string publicKey)
        {
            TryToGetPreviousRoundInformation(out var previousRound);
            var currentBlockTime = Context.CurrentBlockTime;
            var minerInRound = currentRound.RealTimeMinersInformation[publicKey];
            var miningInterval = currentRound.GetMiningInterval();
            var myOrder = currentRound.RealTimeMinersInformation[minerInRound.PublicKey].Order;
            var expectedMiningTime = currentRound.RealTimeMinersInformation[minerInRound.PublicKey].ExpectedMiningTime;

            int nextBlockMiningLeftMilliseconds;
            var hint = new AElfConsensusHint {Behaviour = behaviour}.ToByteString();

            var producedTinyBlocks = minerInRound.ProducedTinyBlocks;

            var duration = expectedMiningTime - currentBlockTime.ToTimestamp();

            Context.LogDebug(() => $"Current behaviour: {behaviour.ToString()}");
            switch (behaviour)
            {
                case AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue:
                    nextBlockMiningLeftMilliseconds = minerInRound.Order * miningInterval;
                    break;
                case AElfConsensusBehaviour.UpdateValue:
                    nextBlockMiningLeftMilliseconds = ConvertDurationToMilliseconds(duration);
                    break;
                case AElfConsensusBehaviour.TinyBlock:
                    if (minerInRound.OutValue != null)
                    {
                        if (currentRound.ExtraBlockProducerOfPreviousRound != publicKey)
                        {
                            expectedMiningTime = expectedMiningTime.ToDateTime().AddMilliseconds(producedTinyBlocks
                                    .Mul(miningInterval).Div(AEDPoSContractConstants.TinyBlocksNumber))
                                .ToTimestamp();
                        }
                        else
                        {
                            // EBP of previous round will produce double tiny blocks. This is for normal time slot of current round.
                            expectedMiningTime = expectedMiningTime.ToDateTime().AddMilliseconds(producedTinyBlocks
                                .Sub(AEDPoSContractConstants.TinyBlocksNumber)
                                .Mul(miningInterval).Div(AEDPoSContractConstants.TinyBlocksNumber)).ToTimestamp();
                        }
                    }
                    else if (previousRound != null)
                    {
                        // EBP of previous round will produce double tiny blocks. This is for extra time slot of previous round.
                        expectedMiningTime = previousRound.GetExtraBlockMiningTime().AddMilliseconds(producedTinyBlocks
                            .Mul(miningInterval).Div(AEDPoSContractConstants.TinyBlocksNumber)).ToTimestamp();
                    }

                    if (currentRound.RoundNumber == 1)
                    {
                        nextBlockMiningLeftMilliseconds =
                            GetNextBlockMiningLeftMillisecondsForFirstRound(minerInRound, currentBlockTime);
                    }
                    else if (currentRound.ExtraBlockProducerOfPreviousRound == publicKey &&
                             producedTinyBlocks < AEDPoSContractConstants.TinyBlocksNumber)
                    {
                        var previousExtraBlockMiningTime =
                            currentRound.RealTimeMinersInformation.Values.First(m => m.Order == 1).ExpectedMiningTime
                                .ToDateTime().AddMilliseconds(-State.MiningInterval.Value).ToTimestamp();
                        nextBlockMiningLeftMilliseconds =
                            GetNextBlockMiningLeftMillisecondsForPreviousRoundExtraBlockProducer(
                                previousExtraBlockMiningTime, producedTinyBlocks, currentBlockTime);
                    }
                    else
                    {
                        nextBlockMiningLeftMilliseconds =
                            ConvertDurationToMilliseconds(expectedMiningTime - currentBlockTime.ToTimestamp());
                    }

                    break;
                case AElfConsensusBehaviour.NextRound:
                    duration = currentRound.ArrangeAbnormalMiningTime(minerInRound.PublicKey, currentBlockTime) -
                               currentBlockTime.ToTimestamp();
                    nextBlockMiningLeftMilliseconds = currentRound.RoundNumber == 1
                        ? currentRound.RealTimeMinersInformation.Count.Mul(miningInterval)
                            .Add(myOrder.Mul(miningInterval))
                        : ConvertDurationToMilliseconds(duration);
                    break;
                case AElfConsensusBehaviour.NextTerm:
                    nextBlockMiningLeftMilliseconds = ConvertDurationToMilliseconds(
                        currentRound.ArrangeAbnormalMiningTime(minerInRound.PublicKey, currentBlockTime) -
                        currentBlockTime.ToTimestamp());
                    break;
                default:
                    return new ConsensusCommand
                    {
                        ExpectedMiningTime = expectedMiningTime,
                        NextBlockMiningLeftMilliseconds = int.MaxValue,
                        LimitMillisecondsOfMiningBlock = int.MaxValue,
                        Hint = new AElfConsensusHint
                        {
                            Behaviour = AElfConsensusBehaviour.Nothing
                        }.ToByteString()
                    };
            }

            Context.LogDebug(() => $"NextBlockMiningLeftMilliseconds: {nextBlockMiningLeftMilliseconds}");

            return new ConsensusCommand
            {
                ExpectedMiningTime = expectedMiningTime,
                NextBlockMiningLeftMilliseconds = nextBlockMiningLeftMilliseconds,
                LimitMillisecondsOfMiningBlock = miningInterval.Div(AEDPoSContractConstants.TinyBlocksNumber),
                Hint = hint
            };
        }
    }
}