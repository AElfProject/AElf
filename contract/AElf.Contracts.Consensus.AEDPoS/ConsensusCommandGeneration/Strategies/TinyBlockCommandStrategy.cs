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
                var firstBlockMiningTime = MinerInRound.ActualMiningTimes.First();
                var arrangedMiningTime = CurrentBlockTime.AddMilliseconds(TinyBlockMinimumInterval);
                var miningDueTime = firstBlockMiningTime.AddMilliseconds(MiningInterval);
                var producedBlocksOfCurrentRound = MinerInRound.ProducedTinyBlocks;

                if (arrangedMiningTime > miningDueTime)
                {
                    return new TerminateRoundCommandStrategy(CurrentRound, Pubkey, CurrentBlockTime, false)
                        .GetAEDPoSConsensusCommand();
                }

                return new ConsensusCommand
                {
                    Hint = new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.TinyBlock}.ToByteString(),
                    ArrangedMiningTime = arrangedMiningTime,
                    MiningDueTime = miningDueTime,
                    LimitMillisecondsOfMiningBlock = producedBlocksOfCurrentRound % _maximumBlocksCount == 0
                        ? LastTinyBlockMiningLimit
                        : DefaultBlockMiningLimit
                };
            }
        }
    }
}