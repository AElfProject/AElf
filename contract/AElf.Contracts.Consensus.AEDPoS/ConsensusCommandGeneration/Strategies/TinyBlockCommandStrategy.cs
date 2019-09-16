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
            private readonly int _maximumBlocksCount;

            public TinyBlockCommandStrategy(Round currentRound, string pubkey, Timestamp currentBlockTime,
                int maximumBlocksCount) : base(
                currentRound, pubkey, currentBlockTime)
            {
                _maximumBlocksCount = maximumBlocksCount;
            }

            public override ConsensusCommand GetAEDPoSConsensusCommand()
            {
                var arrangedMiningTime =
                    MiningTimeArrangingService.ArrangeMiningTimeBasedOnOffset(CurrentBlockTime,
                        TinyBlockMinimumInterval);

                var miningDueTime = MinerInRound.ActualMiningTimes
                    .OrderBy(t => t)
                    .Last()
                    .AddMilliseconds(MiningInterval);

                return arrangedMiningTime > miningDueTime
                    ? new TerminateRoundCommandStrategy(CurrentRound, Pubkey, CurrentBlockTime, false)
                        .GetAEDPoSConsensusCommand()
                    : new ConsensusCommand
                    {
                        Hint = new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.TinyBlock}.ToByteString(),
                        ArrangedMiningTime = arrangedMiningTime,
                        MiningDueTime = miningDueTime,
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