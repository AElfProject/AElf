using AElf.Standards.ACS4;
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
        public class NormalBlockCommandStrategy : CommandStrategyBase
        {
            private readonly long _previousRoundId;

            public NormalBlockCommandStrategy(Round currentRound, string pubkey, Timestamp currentBlockTime,
                long previousRoundId) : base(
                currentRound, pubkey, currentBlockTime)
            {
                _previousRoundId = previousRoundId;
            }

            public override ConsensusCommand GetAEDPoSConsensusCommand()
            {
                var arrangedMiningTime =
                    MiningTimeArrangingService.ArrangeNormalBlockMiningTime(CurrentRound, Pubkey, CurrentBlockTime);

                return new ConsensusCommand
                {
                    Hint = new AElfConsensusHint
                    {
                        Behaviour = AElfConsensusBehaviour.UpdateValue,
                        RoundId = CurrentRound.RoundId,
                        PreviousRoundId = _previousRoundId
                    }.ToByteString(),
                    ArrangedMiningTime = arrangedMiningTime,
                    // Cancel mining after time slot of current miner because of the task queue.
                    MiningDueTime = CurrentRound.GetExpectedMiningTime(Pubkey).AddMilliseconds(MiningInterval),
                    LimitMillisecondsOfMiningBlock = DefaultBlockMiningLimit
                };
            }
        }
    }
}