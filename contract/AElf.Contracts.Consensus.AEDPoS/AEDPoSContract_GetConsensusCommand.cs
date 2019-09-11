using System.Linq;
using Acs4;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        /// <summary>
        /// AElf Consensus Behaviour is changeable in this method when
        /// this miner should skip his time slot more precisely.
        /// </summary>
        /// <param name="behaviour"></param>
        /// <param name="currentRound"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        private ConsensusCommand GetConsensusCommand(AElfConsensusBehaviour behaviour, Round currentRound,
            string publicKey)
        {
            if (AloneMinerDetection(currentRound, publicKey))
                return ConsensusCommandProviderBase.InvalidConsensusCommand;

            var miningInterval = currentRound.GetMiningInterval();
            var currentBlockTime = Context.CurrentBlockTime;
            Timestamp miningDueTime;

            Timestamp arrangedMiningTime;
            int nextBlockMiningLeftMilliseconds;
            if (behaviour == AElfConsensusBehaviour.TinyBlock)
            {
                GetScheduleForTinyBlock(currentRound, publicKey,
                    out nextBlockMiningLeftMilliseconds, out arrangedMiningTime);
                if (nextBlockMiningLeftMilliseconds < 0)
                {
                    Context.LogDebug(() =>
                        "Next block mining left milliseconds is less than 0 for tiny block.");
                    behaviour = AElfConsensusBehaviour.NextRound;
                }
            }

            var minerInRound = currentRound.RealTimeMinersInformation[publicKey];

            switch (behaviour)
            {
                case AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue when currentRound.RoundNumber == 1:
                    // To avoid initial miners fork so fast at the very beginning of current chain.
                    nextBlockMiningLeftMilliseconds = currentRound.GetMiningOrder(publicKey).Mul(currentRound.GetMiningInterval());
                    arrangedMiningTime = Context.CurrentBlockTime.AddMilliseconds(nextBlockMiningLeftMilliseconds);
                    miningDueTime = arrangedMiningTime.AddMilliseconds(miningInterval);
                    break;
                
                case AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue:
                case AElfConsensusBehaviour.UpdateValue:
                    arrangedMiningTime = currentRound.GetExpectedMiningTime(publicKey);
                    // [remove]
                    nextBlockMiningLeftMilliseconds = (int) (arrangedMiningTime - currentBlockTime).Milliseconds();
                    miningDueTime = arrangedMiningTime.AddMilliseconds(miningInterval);
                    break;

                case AElfConsensusBehaviour.NextRound when currentRound.RoundNumber == 1:
                    nextBlockMiningLeftMilliseconds = minerInRound.Order.Add(currentRound.RealTimeMinersInformation.Count)
                        .Sub(1)
                        .Mul(currentRound.GetMiningInterval());
                    arrangedMiningTime = Context.CurrentBlockTime.AddMilliseconds(nextBlockMiningLeftMilliseconds);
                    miningDueTime = arrangedMiningTime.AddMilliseconds(miningInterval);
                    break;
                
                case AElfConsensusBehaviour.NextRound:
                    arrangedMiningTime =
                        currentRound.ArrangeAbnormalMiningTime(minerInRound.Pubkey, Context.CurrentBlockTime);
                    nextBlockMiningLeftMilliseconds = (int) (arrangedMiningTime - Context.CurrentBlockTime).Milliseconds();
                    miningDueTime = arrangedMiningTime.AddMilliseconds(miningInterval);
                    break;
                
                case AElfConsensusBehaviour.NextTerm:
                    arrangedMiningTime = currentRound.ArrangeAbnormalMiningTime(publicKey, currentBlockTime);
                    if (currentRound.RealTimeMinersInformation.Single(m => m.Value.IsExtraBlockProducer).Key !=
                        publicKey)
                    {
                        arrangedMiningTime.AddMilliseconds(miningInterval);
                    }

                    nextBlockMiningLeftMilliseconds = (int) (arrangedMiningTime - currentBlockTime).Milliseconds();
                    miningDueTime = arrangedMiningTime.AddMilliseconds(miningInterval);
                    break;
                case AElfConsensusBehaviour.Nothing:
                    return ConsensusCommandProviderBase.InvalidConsensusCommand;
                default:
                    return ConsensusCommandProviderBase.InvalidConsensusCommand;
            }

            AdjustLimitMillisecondsOfMiningBlock(currentRound, publicKey, nextBlockMiningLeftMilliseconds,
                out var limitMillisecondsOfMiningBlock);

            var milliseconds = nextBlockMiningLeftMilliseconds;
            Context.LogDebug(() => $"NextBlockMiningLeftMilliseconds: {milliseconds}");

            // Produce tiny blocks as fast as one can.
            if (behaviour == AElfConsensusBehaviour.TinyBlock)
            {
                nextBlockMiningLeftMilliseconds = AEDPoSContractConstants.MinimumIntervalOfProducingBlocks;
            }

            if (currentRound.RoundNumber == 1)
            {
                miningDueTime = new Timestamp {Seconds = long.MaxValue};
            }

            return new ConsensusCommand
            {
                ArrangedMiningTime = arrangedMiningTime,
                NextBlockMiningLeftMilliseconds =
                    nextBlockMiningLeftMilliseconds < 0 ? 0 : nextBlockMiningLeftMilliseconds,
                LimitMillisecondsOfMiningBlock = behaviour == AElfConsensusBehaviour.NextTerm
                    ? miningInterval.Mul(AEDPoSContractConstants.LimitBlockExecutionTimeWeight)
                        .Div(AEDPoSContractConstants.LimitBlockExecutionTimeTotalShares)
                    : limitMillisecondsOfMiningBlock,
                Hint = new AElfConsensusHint {Behaviour = behaviour}.ToByteString(),
                MiningDueTime = miningDueTime
            };
        }

        /// <summary>
        /// If current miner mined blocks only himself for 2 rounds,
        /// just stop and waiting to execute other miners' blocks.
        /// </summary>
        /// <param name="currentRound"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        private bool AloneMinerDetection(Round currentRound, string publicKey)
        {
            var isAlone = false;
            if (currentRound.RoundNumber > 3 && currentRound.RealTimeMinersInformation.Count > 2)
            {
                // Not single node.

                // If only this node mined during previous round, stop mining.
                if (TryToGetPreviousRoundInformation(out var previousRound))
                {
                    var minedMiners = previousRound.GetMinedMiners();
                    isAlone = minedMiners.Count == 1 &&
                              minedMiners.Select(m => m.Pubkey).Contains(publicKey);
                }

                // check one Further round.
                if (isAlone && TryToGetRoundInformation(previousRound.RoundNumber.Sub(1),
                        out var previousPreviousRound))
                {
                    var minedMiners = previousPreviousRound.GetMinedMiners();
                    isAlone = minedMiners.Count == 1 &&
                              minedMiners.Select(m => m.Pubkey).Contains(publicKey);
                }
            }

            return isAlone;
        }

        #region Get next block mining left milliseconds
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

            if (minerInRound.IsThisANewRoundForThisMiner())
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
                currentRound.RoundNumber == 2 && !minerInRound.IsThisANewRoundForThisMiner())
            {
                nextBlockMiningLeftMilliseconds =
                    GetNextBlockMiningLeftMillisecondsForFirstRound(minerInRound, miningInterval);
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

            limitMillisecondsOfMiningBlock = miningInterval.Div(AEDPoSContractConstants.TotalTinySlots); //.Add(offset);
            limitMillisecondsOfMiningBlock = limitMillisecondsOfMiningBlock < 0 ? 0 : limitMillisecondsOfMiningBlock;

            var currentRoundStartTime = currentRound.GetStartTime();
            var producedTinyBlocksForPreviousRound =
                minerInRound.ActualMiningTimes.Count(t => t < currentRoundStartTime);

            var blocksCount = GetMaximumBlocksCount();
            if (minerInRound.ProducedTinyBlocks == blocksCount ||
                minerInRound.ProducedTinyBlocks == blocksCount.Add(producedTinyBlocksForPreviousRound))
            {
                limitMillisecondsOfMiningBlock = limitMillisecondsOfMiningBlock.Div(2);
            }
            else
            {
                limitMillisecondsOfMiningBlock = limitMillisecondsOfMiningBlock
                    .Mul(AEDPoSContractConstants.LimitBlockExecutionTimeWeight)
                    .Div(AEDPoSContractConstants.LimitBlockExecutionTimeTotalShares);
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
    }
}