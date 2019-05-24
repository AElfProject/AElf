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
        /// Get next consensus behaviour of the caller based on current state.
        /// This method can be tested by testing GetConsensusCommand.
        /// </summary>
        /// <param name="currentRound"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        private AElfConsensusBehaviour GetBehaviour(Round currentRound, string publicKey)
        {
            if (!currentRound.RealTimeMinersInformation.ContainsKey(publicKey))
            {
                return AElfConsensusBehaviour.Nothing;
            }

            var currentBlockTime = Context.CurrentBlockTime;
            var isPreviousRoundExists = TryToGetPreviousRoundInformation(out var previousRound);
            var isTermJustChanged = IsJustChangedTerm(out var termNumber);
            var isTimeSlotPassed = currentRound.IsTimeSlotPassed(publicKey, currentBlockTime, out var minerInRound);

            if (minerInRound.OutValue == null)
            {
                var behaviour = GetBehaviourIfMinerDoesNotProduceBlockInCurrentRound(currentRound, minerInRound,
                    isPreviousRoundExists, isTermJustChanged, isTimeSlotPassed);
                if (behaviour != AElfConsensusBehaviour.Nothing)
                {
                    return behaviour;
                }
            }
            else if (!isTimeSlotPassed &&
                     minerInRound.ProducedTinyBlocks < AEDPoSContractConstants.TinyBlocksNumber)
            {
                return AElfConsensusBehaviour.TinyBlock;
            }
            else if (!isTimeSlotPassed &&
                     currentRound.ExtraBlockProducerOfPreviousRound == publicKey &&
                     !isTermJustChanged &&
                     minerInRound.ProducedTinyBlocks < AEDPoSContractConstants.TinyBlocksNumber.Mul(2))
            {
                return AElfConsensusBehaviour.TinyBlock;
            }

            // Side chain will go next round directly.
            return State.TimeEachTerm.Value == int.MaxValue
                ? AElfConsensusBehaviour.NextRound
                : GetBehaviourForChainAbleToChangeTerm(currentRound, previousRound, termNumber);
        }

        private AElfConsensusBehaviour GetBehaviourIfMinerDoesNotProduceBlockInCurrentRound(Round currentRound,
            MinerInRound minerInRound, bool isPreviousRoundExists, bool isTermJustChanged, bool isTimeSlotPassed)
        {
            if (!isPreviousRoundExists && minerInRound.Order != 1 &&
                currentRound.RealTimeMinersInformation.Values.First(m => m.Order == 1).OutValue == null)
            {
                // In first round, if block of boot node not executed, don't produce block to
                // avoid forks creating.
                return AElfConsensusBehaviour.NextRound;
            }

            if (!isPreviousRoundExists || isTermJustChanged)
            {
                // Failed to get previous round information or just changed term.
                return AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue;
            }

            if (currentRound.ExtraBlockProducerOfPreviousRound == minerInRound.PublicKey &&
                Context.CurrentBlockTime < currentRound.GetStartTime() &&
                minerInRound.ProducedTinyBlocks < AEDPoSContractConstants.TinyBlocksNumber)
            {
                return AElfConsensusBehaviour.TinyBlock;
            }

            return !isTimeSlotPassed ? AElfConsensusBehaviour.UpdateValue : AElfConsensusBehaviour.Nothing;
        }

        private AElfConsensusBehaviour GetBehaviourForChainAbleToChangeTerm(Round currentRound, Round previousRound,
            long termNumber)
        {
            // In first round, the blockchain start timestamp is incorrect.
            // We can return NextRound directly.
            if (currentRound.RoundNumber == 1)
            {
                return AElfConsensusBehaviour.NextRound;
            }

            Assert(TryToGetBlockchainStartTimestamp(out var blockchainStartTimestamp),
                "Failed to get blockchain start timestamp.");

            Context.LogDebug(() => $"Using start timestamp: {blockchainStartTimestamp}");

            // Calculate the approvals and make the judgement of changing term.
            return currentRound.IsTimeToChangeTerm(previousRound, blockchainStartTimestamp, termNumber,
                State.TimeEachTerm.Value)
                ? AElfConsensusBehaviour.NextTerm
                : AElfConsensusBehaviour.NextRound;
        }

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
            var currentBlockTime = Context.CurrentBlockTime;
            var minerInRound = currentRound.RealTimeMinersInformation[publicKey];
            var miningInterval = currentRound.GetMiningInterval();
            var expectedMiningTime = currentRound.RealTimeMinersInformation[minerInRound.PublicKey].ExpectedMiningTime;
            var duration = expectedMiningTime - currentBlockTime.ToTimestamp();
            int nextBlockMiningLeftMilliseconds;
            switch (behaviour)
            {
                case AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue:
                    if (currentRound.RoundNumber == 1 &&
                        currentRound.RealTimeMinersInformation.Values.First(m => m.Order == 1).OutValue == null)
                    {
                        // To avoid the initial miners fork so fast at the very beginning.
                        nextBlockMiningLeftMilliseconds = minerInRound.Order * miningInterval;
                    }
                    else
                    {
                        nextBlockMiningLeftMilliseconds = ConvertDurationToMilliseconds(duration);
                    }

                    break;
                case AElfConsensusBehaviour.UpdateValue:
                    nextBlockMiningLeftMilliseconds = ConvertDurationToMilliseconds(duration);
                    break;
                case AElfConsensusBehaviour.TinyBlock:
                    GetNextBlockMiningLeftMillisecondsForTinyBlock(currentRound, publicKey,
                        out nextBlockMiningLeftMilliseconds);
                    break;
                case AElfConsensusBehaviour.NextRound:
                    expectedMiningTime =
                        currentRound.ArrangeAbnormalMiningTime(minerInRound.PublicKey, currentBlockTime);
                    duration = expectedMiningTime - currentBlockTime.ToTimestamp();
                    nextBlockMiningLeftMilliseconds = ConvertDurationToMilliseconds(duration);
                    break;
                case AElfConsensusBehaviour.NextTerm:
                    expectedMiningTime =
                        currentRound.ArrangeAbnormalMiningTime(minerInRound.PublicKey, currentBlockTime);
                    nextBlockMiningLeftMilliseconds =
                        ConvertDurationToMilliseconds(expectedMiningTime - currentBlockTime.ToTimestamp());
                    break;
                default:
                    return new ConsensusCommand
                    {
                        ExpectedMiningTime = expectedMiningTime,
                        NextBlockMiningLeftMilliseconds = int.MaxValue,
                        LimitMillisecondsOfMiningBlock = int.MaxValue,
                        Hint = new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.Nothing}.ToByteString()
                    };
            }

            Context.LogDebug(() => $"NextBlockMiningLeftMilliseconds: {nextBlockMiningLeftMilliseconds}");

            return new ConsensusCommand
            {
                ExpectedMiningTime = expectedMiningTime,
                NextBlockMiningLeftMilliseconds = nextBlockMiningLeftMilliseconds,
                LimitMillisecondsOfMiningBlock = behaviour == AElfConsensusBehaviour.NextTerm
                    ? miningInterval
                    : miningInterval.Div(AEDPoSContractConstants.TotalTinySlots),
                Hint = new AElfConsensusHint {Behaviour = behaviour}.ToByteString()
            };
        }

        private void GetNextBlockMiningLeftMillisecondsForTinyBlock(Round currentRound, string publicKey,
            out int nextBlockMiningLeftMilliseconds)
        {
            var minerInRound = currentRound.RealTimeMinersInformation[publicKey];
            var producedTinyBlocks = minerInRound.ProducedTinyBlocks;
            var currentBlockTime = Context.CurrentBlockTime;
            var miningInterval = currentRound.GetMiningInterval();
            TryToGetPreviousRoundInformation(out var previousRound);
            var expectedMiningTime = currentRound.RealTimeMinersInformation[minerInRound.PublicKey].ExpectedMiningTime;

            if (minerInRound.OutValue != null)
            {
                if (currentRound.ExtraBlockProducerOfPreviousRound != publicKey)
                {
                    expectedMiningTime = expectedMiningTime.ToDateTime().AddMilliseconds(producedTinyBlocks
                            .Mul(miningInterval).Div(AEDPoSContractConstants.TotalTinySlots))
                        .ToTimestamp();
                }
                else
                {
                    // EBP of previous round will produce double tiny blocks. This is for normal time slot of current round.
                    expectedMiningTime = expectedMiningTime.ToDateTime().AddMilliseconds(producedTinyBlocks
                        .Sub(AEDPoSContractConstants.TinyBlocksNumber)
                        .Mul(miningInterval).Div(AEDPoSContractConstants.TotalTinySlots)).ToTimestamp();
                }
            }
            else if (previousRound != null)
            {
                // EBP of previous round will produce double tiny blocks. This is for extra time slot of previous round.
                expectedMiningTime = previousRound.GetExtraBlockMiningTime().AddMilliseconds(producedTinyBlocks
                    .Mul(miningInterval).Div(AEDPoSContractConstants.TotalTinySlots)).ToTimestamp();
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

            if (minerInRound.Order >= currentRound.RealTimeMinersInformation.Count) return;

            var nextMinerInRound =
                currentRound.RealTimeMinersInformation.Values.First(m => m.Order == minerInRound.Order.Add(1));
            if (nextMinerInRound.ActualMiningTimes.Any(t => t > nextMinerInRound.ExpectedMiningTime))
            {
                nextBlockMiningLeftMilliseconds = int.MaxValue;
            }
        }
    }
}