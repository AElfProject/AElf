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
            while (true)
            {
                var isAlone = CheckLonelyMiner(publicKey);
                if (behaviour == AElfConsensusBehaviour.TinyBlock && isAlone && 
                    currentRound.RealTimeMinersInformation.Count > 2 // There are more than 1 miner possible to save him.
                    )
                {
                    behaviour = AElfConsensusBehaviour.Nothing;
                }

                var currentBlockTime = Context.CurrentBlockTime;
                Timestamp expectedMiningTime;
                int nextBlockMiningLeftMilliseconds;

                switch (behaviour)
                {
                    case AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue:
                        GetScheduleForUpdateValueWithoutPreviousInValue(currentRound, publicKey,
                            out nextBlockMiningLeftMilliseconds, out expectedMiningTime);
                        break;
                    case AElfConsensusBehaviour.UpdateValue:
                        expectedMiningTime = currentRound.GetExpectedMiningTime(publicKey);
                        nextBlockMiningLeftMilliseconds = (int) (expectedMiningTime - currentBlockTime).Milliseconds();
                        break;
                    case AElfConsensusBehaviour.TinyBlock:
                        GetScheduleForTinyBlock(currentRound, publicKey,
                            out nextBlockMiningLeftMilliseconds, out expectedMiningTime);
                        if (nextBlockMiningLeftMilliseconds < 0)
                        {
                            Context.LogDebug(() =>
                                "Next block mining left milliseconds is less than 0 for tiny block.");
                            behaviour = AElfConsensusBehaviour.NextRound;
                            continue;
                        }

                        break;
                    case AElfConsensusBehaviour.NextRound:
                        GetScheduleForNextRound(currentRound, publicKey,
                            out nextBlockMiningLeftMilliseconds, out expectedMiningTime);
                        break;
                    case AElfConsensusBehaviour.NextTerm:
                        expectedMiningTime = currentRound.ArrangeAbnormalMiningTime(publicKey, currentBlockTime);
                        nextBlockMiningLeftMilliseconds = (int) (expectedMiningTime - currentBlockTime).Milliseconds();
                        break;
                    case AElfConsensusBehaviour.Nothing:
                        return GetInvalidConsensusCommand();
                    default:
                        return GetInvalidConsensusCommand();
                }

                AdjustLimitMillisecondsOfMiningBlock(currentRound, publicKey, nextBlockMiningLeftMilliseconds,
                    out var limitMillisecondsOfMiningBlock);

                var milliseconds = nextBlockMiningLeftMilliseconds;
                Context.LogDebug(() => $"NextBlockMiningLeftMilliseconds: {milliseconds}");

                var miningInterval = currentRound.GetMiningInterval();
                // Produce tiny blocks as fast as one can.
                if (behaviour == AElfConsensusBehaviour.TinyBlock)
                {
                    nextBlockMiningLeftMilliseconds = AEDPoSContractConstants.TimeForNetwork;
                }

                return new ConsensusCommand
                {
                    ExpectedMiningTime = expectedMiningTime,
                    NextBlockMiningLeftMilliseconds =
                        nextBlockMiningLeftMilliseconds < 0 ? 0 : nextBlockMiningLeftMilliseconds,
                    LimitMillisecondsOfMiningBlock = isAlone
                        ? currentRound.GetMiningInterval()
                        : behaviour == AElfConsensusBehaviour.NextTerm
                            ? miningInterval.Div(2)
                            : limitMillisecondsOfMiningBlock,
                    Hint = new AElfConsensusHint {Behaviour = behaviour}.ToByteString()
                };
            }
        }

        #region Get next block mining left milliseconds

        private void GetScheduleForUpdateValueWithoutPreviousInValue(Round currentRound,
            string publicKey, out int nextBlockMiningLeftMilliseconds, out Timestamp expectedMiningTime)
        {
            if (currentRound.RoundNumber == 1)
            {
                // To avoid initial miners fork so fast at the very beginning of current chain.
                nextBlockMiningLeftMilliseconds =
                    currentRound.GetMiningOrder(publicKey).Mul(currentRound.GetMiningInterval());
                expectedMiningTime = Context.CurrentBlockTime.AddMilliseconds(nextBlockMiningLeftMilliseconds);
            }
            else
            {
                // As normal as case AElfConsensusBehaviour.UpdateValue.
                expectedMiningTime = currentRound.GetExpectedMiningTime(publicKey);
                nextBlockMiningLeftMilliseconds = (int) (expectedMiningTime - Context.CurrentBlockTime).Milliseconds();
            }
        }

        /// <summary>
        /// We have 2 cases of producing tiny blocks:
        /// 1. After generating information of next round (producing extra block)
        /// 2. After publishing out value (producing normal block)
        /// </summary>
        /// <param name="currentRound"></param>
        /// <param name="publicKey"></param>
        /// <param name="nextBlockMiningLeftMilliseconds"></param>
        /// <param name="expectedMiningTime"></param>
        private void GetScheduleForTinyBlock(Round currentRound, string publicKey,
            out int nextBlockMiningLeftMilliseconds, out Timestamp expectedMiningTime)
        {
            var minerInRound = currentRound.RealTimeMinersInformation[publicKey];
            var producedTinyBlocks = minerInRound.ProducedTinyBlocks;
            var currentRoundStartTime = currentRound.GetStartTime();
            var producedTinyBlocksForPreviousRound =
                minerInRound.ActualMiningTimes.Count(t => t < currentRoundStartTime);
            var miningInterval = currentRound.GetMiningInterval();
            var timeForEachBlock = miningInterval.Div(AEDPoSContractConstants.TotalTinySlots);
            expectedMiningTime = currentRound.GetExpectedMiningTime(publicKey);

            if (minerInRound.IsMinedBlockForCurrentRound())
            {
                // After publishing out value (producing normal block)
                expectedMiningTime = expectedMiningTime.AddMilliseconds(
                    currentRound.ExtraBlockProducerOfPreviousRound != publicKey
                        ? producedTinyBlocks.Mul(timeForEachBlock)
                        // Previous extra block producer can produce double tiny blocks at most.
                        : producedTinyBlocks.Sub(producedTinyBlocksForPreviousRound).Mul(timeForEachBlock));
            }
            else if (TryToGetPreviousRoundInformation(out _))
            {
                // After generating information of next round (producing extra block)
                expectedMiningTime = currentRound.GetStartTime().AddMilliseconds(-miningInterval)
                    .AddMilliseconds(producedTinyBlocks.Mul(timeForEachBlock));
            }

            if (currentRound.RoundNumber == 1 ||
                currentRound.RoundNumber == 2 && !minerInRound.IsMinedBlockForCurrentRound())
            {
                nextBlockMiningLeftMilliseconds = GetNextBlockMiningLeftMillisecondsForFirstRound(minerInRound, miningInterval);
            }
            else
            {
                TuneExpectedMiningTimeForTinyBlock(miningInterval,
                    currentRound.GetExpectedMiningTime(publicKey),
                    ref expectedMiningTime);

                nextBlockMiningLeftMilliseconds = (int) (expectedMiningTime - Context.CurrentBlockTime).Milliseconds();

                var toPrint = expectedMiningTime;
                Context.LogDebug(() =>
                    $"expected mining time: {toPrint}, current block time: {Context.CurrentBlockTime}. " +
                    $"next: {(int) (toPrint - Context.CurrentBlockTime).Milliseconds()}");
            }
        }

        private void GetScheduleForNextRound(Round currentRound, string publicKey,
            out int nextBlockMiningLeftMilliseconds, out Timestamp expectedMiningTime)
        {
            var minerInRound = currentRound.RealTimeMinersInformation[publicKey];
            if (currentRound.RoundNumber == 1)
            {
                nextBlockMiningLeftMilliseconds = minerInRound.Order.Add(currentRound.RealTimeMinersInformation.Count)
                    .Sub(1)
                    .Mul(currentRound.GetMiningInterval());
                expectedMiningTime = Context.CurrentBlockTime.AddMilliseconds(nextBlockMiningLeftMilliseconds);
            }
            else
            {
                expectedMiningTime =
                    currentRound.ArrangeAbnormalMiningTime(minerInRound.Pubkey, Context.CurrentBlockTime);
                nextBlockMiningLeftMilliseconds = (int) (expectedMiningTime - Context.CurrentBlockTime).Milliseconds();
            }
        }

        #endregion

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
            return (int) (expectedMiningTime - Context.CurrentBlockTime).Milliseconds();
        }

        private void AdjustLimitMillisecondsOfMiningBlock(Round currentRound, string publicKey,
            int nextBlockMiningLeftMilliseconds, out int limitMillisecondsOfMiningBlock)
        {
            var minerInRound = currentRound.RealTimeMinersInformation[publicKey];
            var miningInterval = currentRound.GetMiningInterval();
            var offset = 0;
            if (nextBlockMiningLeftMilliseconds < 0)
            {
                Context.LogDebug(() => "Next block mining left milliseconds is less than 0.");
                offset = nextBlockMiningLeftMilliseconds;
            }

            limitMillisecondsOfMiningBlock = miningInterval.Div(AEDPoSContractConstants.TotalTinySlots).Add(offset);
            limitMillisecondsOfMiningBlock = limitMillisecondsOfMiningBlock < 0 ? 0 : limitMillisecondsOfMiningBlock;

            var currentRoundStartTime = currentRound.GetStartTime();
            var producedTinyBlocksForPreviousRound =
                minerInRound.ActualMiningTimes.Count(t => t < currentRoundStartTime);

            if (minerInRound.ProducedTinyBlocks == AEDPoSContractConstants.TinyBlocksNumber ||
                minerInRound.ProducedTinyBlocks ==
                AEDPoSContractConstants.TinyBlocksNumber.Add(producedTinyBlocksForPreviousRound))
            {
                limitMillisecondsOfMiningBlock = limitMillisecondsOfMiningBlock.Div(2);
            }
            else
            {
                limitMillisecondsOfMiningBlock = limitMillisecondsOfMiningBlock
                    .Mul(AEDPoSContractConstants.LimitBlockExecutionTimeWeight)
                    .Div(AEDPoSContractConstants.LimitBlockExecutionTimeTotalWeight);
            }
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
            var currentBlockTime = Context.CurrentBlockTime;
            while (expectedMiningTime < currentBlockTime &&
                   expectedMiningTime < originExpectedMiningTime.AddMilliseconds(miningInterval))
            {
                expectedMiningTime = expectedMiningTime.AddMilliseconds(timeForEachBlock);
                var toPrint = expectedMiningTime.Clone();
                Context.LogDebug(() => $"Moving to next tiny block time slot. {toPrint}");
            }
        }

        private ConsensusCommand GetInvalidConsensusCommand() => new ConsensusCommand
        {
            ExpectedMiningTime = new Timestamp {Seconds = long.MaxValue},
            Hint = ByteString.CopyFrom(new AElfConsensusHint
            {
                Behaviour = AElfConsensusBehaviour.Nothing
            }.ToByteArray()),
            LimitMillisecondsOfMiningBlock = 0,
            NextBlockMiningLeftMilliseconds = int.MaxValue
        };

        private bool CheckLonelyMiner(string publicKey)
        {
            if (TryToGetPreviousRoundInformation(out var previousRound))
            {
                var minedMiners = previousRound.GetMinedMiners();
                return minedMiners.Count == 1 &&
                       minedMiners.Select(m => m.Pubkey).Contains(publicKey);
            }

            return false;
        }
    }
}