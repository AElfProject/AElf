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
                    isPreviousRoundExists, isTermJustChanged);

                if (!isTimeSlotPassed && behaviour == AElfConsensusBehaviour.Nothing)
                    behaviour = AElfConsensusBehaviour.UpdateValue;

                if (behaviour != AElfConsensusBehaviour.Nothing)
                    return behaviour;
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
            MinerInRound minerInRound, bool isPreviousRoundExists, bool isTermJustChanged)
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

            return AElfConsensusBehaviour.Nothing;
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
                    GetNextBlockMiningLeftMillisecondsForUpdateValueWithoutPreviousInValue(currentRound, publicKey,
                        duration, out nextBlockMiningLeftMilliseconds);
                    break;
                case AElfConsensusBehaviour.UpdateValue:
                    nextBlockMiningLeftMilliseconds = ConvertDurationToMilliseconds(duration);
                    break;
                case AElfConsensusBehaviour.TinyBlock:
                    GetNextBlockMiningLeftMillisecondsForTinyBlock(currentRound, publicKey,
                        out nextBlockMiningLeftMilliseconds, out expectedMiningTime);
                    if (nextBlockMiningLeftMilliseconds < 0)
                    {
                        return GetConsensusCommand(AElfConsensusBehaviour.NextRound, currentRound, publicKey);
                    }

                    break;
                case AElfConsensusBehaviour.NextRound:
                    GetNextBlockMiningLeftMillisecondsForNextRound(currentRound, publicKey,
                        out nextBlockMiningLeftMilliseconds, out expectedMiningTime);
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

            AdjustLimitMillisecondsOfMiningBlock(nextBlockMiningLeftMilliseconds, minerInRound, miningInterval,
                out var limitMillisecondsOfMiningBlock);

            return new ConsensusCommand
            {
                ExpectedMiningTime = expectedMiningTime,
                NextBlockMiningLeftMilliseconds = nextBlockMiningLeftMilliseconds,
                LimitMillisecondsOfMiningBlock = behaviour == AElfConsensusBehaviour.NextTerm
                    ? miningInterval.Div(2)
                    : limitMillisecondsOfMiningBlock.Mul(AEDPoSContractConstants.LimitBlockExecutionTimeWeight)
                        .Div(AEDPoSContractConstants.LimitBlockExecutionTimeTotalWeight),
                Hint = new AElfConsensusHint {Behaviour = behaviour}.ToByteString()
            };
        }

        private void AdjustLimitMillisecondsOfMiningBlock(int nextBlockMiningLeftMilliseconds,
            MinerInRound minerInRound, int miningInterval, out int limitMillisecondsOfMiningBlock)
        {
            var offset = 0;
            if (nextBlockMiningLeftMilliseconds < 0)
            {
                offset = nextBlockMiningLeftMilliseconds;
            }

            limitMillisecondsOfMiningBlock = miningInterval.Div(AEDPoSContractConstants.TotalTinySlots).Add(offset);
            limitMillisecondsOfMiningBlock = limitMillisecondsOfMiningBlock < 0 ? 0 : limitMillisecondsOfMiningBlock;

            if (minerInRound.ProducedTinyBlocks == AEDPoSContractConstants.TinyBlocksNumber ||
                minerInRound.ProducedTinyBlocks == AEDPoSContractConstants.TinyBlocksNumber.Mul(2))
            {
                limitMillisecondsOfMiningBlock = limitMillisecondsOfMiningBlock.Div(2);
            }
        }

        private void GetNextBlockMiningLeftMillisecondsForUpdateValueWithoutPreviousInValue(Round currentRound,
            string publicKey, Duration duration, out int nextBlockMiningLeftMilliseconds)
        {
            var minerInRound = currentRound.RealTimeMinersInformation[publicKey];
            var miningInterval = currentRound.GetMiningInterval();
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
        }

        private void GetNextBlockMiningLeftMillisecondsForNextRound(Round currentRound,
            string publicKey, out int nextBlockMiningLeftMilliseconds, out Timestamp expectedMiningTime)
        {
            var minerInRound = currentRound.RealTimeMinersInformation[publicKey];
            if (currentRound.RoundNumber == 1 && minerInRound.Order != 1)
            {
                nextBlockMiningLeftMilliseconds = minerInRound.Order.Add(currentRound.RealTimeMinersInformation.Count)
                    .Mul(currentRound.GetMiningInterval());
                expectedMiningTime = Context.CurrentBlockTime.ToTimestamp() +
                                     ConvertMillisecondsToDuration(nextBlockMiningLeftMilliseconds);
            }
            else
            {
                expectedMiningTime =
                    currentRound.ArrangeAbnormalMiningTime(minerInRound.PublicKey, Context.CurrentBlockTime);
                var duration = expectedMiningTime - Context.CurrentBlockTime.ToTimestamp();
                nextBlockMiningLeftMilliseconds = ConvertDurationToMilliseconds(duration);
            }
        }

        private void GetNextBlockMiningLeftMillisecondsForTinyBlock(Round currentRound, string publicKey,
            out int nextBlockMiningLeftMilliseconds, out Timestamp expectedMiningTime)
        {
            var minerInRound = currentRound.RealTimeMinersInformation[publicKey];
            var producedTinyBlocks = minerInRound.ProducedTinyBlocks;
            var currentBlockTime = Context.CurrentBlockTime;
            var miningInterval = currentRound.GetMiningInterval();
            TryToGetPreviousRoundInformation(out var previousRound);
            expectedMiningTime = currentRound.RealTimeMinersInformation[minerInRound.PublicKey].ExpectedMiningTime;

            if (minerInRound.OutValue != null)
            {
                if (currentRound.ExtraBlockProducerOfPreviousRound != publicKey)
                {
                    expectedMiningTime = expectedMiningTime + new Duration
                    {
                        Seconds = producedTinyBlocks
                            .Mul(miningInterval).Div(AEDPoSContractConstants.TotalTinySlots).Div(1000)
                    };
                }
                else
                {
                    // EBP of previous round will produce double tiny blocks. This is for normal time slot of current round.
                    expectedMiningTime = expectedMiningTime + new Duration
                    {
                        Seconds = producedTinyBlocks
                            .Sub(AEDPoSContractConstants.TinyBlocksNumber)
                            .Mul(miningInterval).Div(AEDPoSContractConstants.TotalTinySlots).Div(1000)
                    };
                }
            }
            else if (previousRound != null)
            {
                // EBP of previous round will produce double tiny blocks. This is for extra time slot of previous round.
                expectedMiningTime = previousRound.GetExtraBlockMiningTime().ToTimestamp() + new Duration
                {
                    Seconds = producedTinyBlocks
                        .Mul(miningInterval).Div(AEDPoSContractConstants.TotalTinySlots).Div(1000)
                };
            }

            if (currentRound.RoundNumber == 1 || (currentRound.RoundNumber == 2 && minerInRound.OutValue == null))
            {
                nextBlockMiningLeftMilliseconds =
                    GetNextBlockMiningLeftMillisecondsForFirstRound(minerInRound, currentBlockTime);
            }
            else
            {
                TuneExpectedMiningTimeForTinyBlock(miningInterval, ref expectedMiningTime);
                TuneNextBlockMiningLeftMillisecondsForTinyBlock(currentRound, publicKey, expectedMiningTime,
                    out nextBlockMiningLeftMilliseconds);
            }
        }

        private void TuneNextBlockMiningLeftMillisecondsForTinyBlock(Round currentRound, string publicKey,
            Timestamp expectedMiningTime,
            out int nextBlockMiningLeftMilliseconds)
        {
            var minerInRound = currentRound.RealTimeMinersInformation[publicKey];
            var producedTinyBlocks = minerInRound.ProducedTinyBlocks;
            var miningInterval = currentRound.GetMiningInterval();

            if (currentRound.ExtraBlockProducerOfPreviousRound == publicKey &&
                producedTinyBlocks < AEDPoSContractConstants.TinyBlocksNumber)
            {
                var previousExtraBlockMiningTime =
                    currentRound.RealTimeMinersInformation.Values.First(m => m.Order == 1).ExpectedMiningTime +
                    new Duration
                    {
                        Seconds = -State.MiningInterval.Value.Div(1000)
                    };
                nextBlockMiningLeftMilliseconds =
                    GetNextBlockMiningLeftMillisecondsForPreviousRoundExtraBlockProducer(
                        previousExtraBlockMiningTime, producedTinyBlocks, Context.CurrentBlockTime);
            }
            else
            {
                nextBlockMiningLeftMilliseconds =
                    ConvertDurationToMilliseconds(expectedMiningTime - Context.CurrentBlockTime.ToTimestamp());
            }

            if (Context.CurrentBlockTime.ToTimestamp() >=
                minerInRound.ExpectedMiningTime + new Duration {Seconds = miningInterval.Div(1000)})
            {
                nextBlockMiningLeftMilliseconds = int.MinValue;
            }
        }

        private void TuneExpectedMiningTimeForTinyBlock(int miningInterval, ref Timestamp expectedMiningTime)
        {
            var currentBlockTime = Context.CurrentBlockTime.ToTimestamp();
            var step = ConvertMillisecondsToDuration(
                State.MiningInterval.Value.Div(AEDPoSContractConstants.TotalTinySlots));
            while (currentBlockTime > expectedMiningTime && Context.CurrentBlockTime.ToTimestamp() <
                   expectedMiningTime + new Duration {Seconds = miningInterval.Div(1000)})
            {
                var toPrint = expectedMiningTime.Clone();
                Context.LogDebug(() => $"Moving to next tiny block time slot. {toPrint}");
                expectedMiningTime = expectedMiningTime + step;
            }
        }
    }
}