using System.Linq;
using AElf.Standards.ACS4;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        private class TinyBlockCommandStrategy : CommandStrategyBase
        {
            private readonly int _maximumBlocksCount;

            public TinyBlockCommandStrategy(Round currentRound, string pubkey, Timestamp currentBlockTime,
                int maximumBlocksCount) : base(
                currentRound, pubkey, currentBlockTime)
            {
                _maximumBlocksCount = maximumBlocksCount;
            }

            public override ConsensusCommand GetAEDPoSConsensusCommand()
            {
                // Provided pubkey can mine a block after TinyBlockMinimumInterval ms.
                var arrangedMiningTime =
                    MiningTimeArrangingService.ArrangeMiningTimeWithOffset(CurrentBlockTime,
                        TinyBlockMinimumInterval);

                var roundStartTime = CurrentRound.GetRoundStartTime();
                var currentTimeSlotStartTime = CurrentBlockTime < roundStartTime
                    ? roundStartTime.AddMilliseconds(-MiningInterval)
                    : CurrentRound.RoundNumber == 1
                        ? MinerInRound.ActualMiningTimes.First()
                        : MinerInRound.ExpectedMiningTime;
                var currentTimeSlotEndTime = currentTimeSlotStartTime.AddMilliseconds(MiningInterval);

                return arrangedMiningTime > currentTimeSlotEndTime
                    ? new TerminateRoundCommandStrategy(CurrentRound, Pubkey, CurrentBlockTime, false)
                        .GetAEDPoSConsensusCommand() // The arranged mining time already beyond the time slot.
                    : new ConsensusCommand
                    {
                        Hint = new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.TinyBlock}.ToByteString(),
                        ArrangedMiningTime = arrangedMiningTime,
                        MiningDueTime = currentTimeSlotEndTime,
                        LimitMillisecondsOfMiningBlock = IsLastTinyBlockOfCurrentSlot()
                            ? LastTinyBlockMiningLimit
                            : DefaultBlockMiningLimit
                    };
            }

            private bool IsLastTinyBlockOfCurrentSlot()
            {
                var producedBlocksOfCurrentRound = MinerInRound.ProducedTinyBlocks;
                var roundStartTime = CurrentRound.GetRoundStartTime();

                if (CurrentBlockTime < roundStartTime)
                {
                    return producedBlocksOfCurrentRound == _maximumBlocksCount;
                }

                var blocksBeforeCurrentRound = MinerInRound.ActualMiningTimes.Count(t => t < roundStartTime);
                return producedBlocksOfCurrentRound == blocksBeforeCurrentRound.Add(_maximumBlocksCount);
            }
        }
    }
}