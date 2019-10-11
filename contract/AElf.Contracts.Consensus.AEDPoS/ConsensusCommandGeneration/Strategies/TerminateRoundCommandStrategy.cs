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
        public class TerminateRoundCommandStrategy : CommandStrategyBase
        {
            private readonly bool _isNewTerm;

            public TerminateRoundCommandStrategy(Round currentRound, string pubkey, Timestamp currentBlockTime,
                bool isNewTerm) : base(
                currentRound, pubkey, currentBlockTime)
            {
                _isNewTerm = isNewTerm;
            }

            public override ConsensusCommand GetAEDPoSConsensusCommand()
            {
                var arrangedMiningTime =
                    MiningTimeArrangingService.ArrangeExtraBlockMiningTime(CurrentRound, Pubkey, CurrentBlockTime);
                return new ConsensusCommand
                {
                    Hint = new AElfConsensusHint
                        {
                            Behaviour = _isNewTerm ? AElfConsensusBehaviour.NextTerm : AElfConsensusBehaviour.NextRound
                        }
                        .ToByteString(),
                    ArrangedMiningTime = arrangedMiningTime,
                    MiningDueTime = arrangedMiningTime.AddMilliseconds(MiningInterval),
                    LimitMillisecondsOfMiningBlock =
                        _isNewTerm ? LastBlockOfCurrentTermMiningLimit : DefaultBlockMiningLimit
                };
            }
        }
    }
}