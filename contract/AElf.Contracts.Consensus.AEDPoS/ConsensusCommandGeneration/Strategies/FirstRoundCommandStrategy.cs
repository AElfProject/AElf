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
        public class FirstRoundCommandStrategy : CommandStrategyBase
        {
            private readonly AElfConsensusBehaviour _consensusBehaviour;

            public FirstRoundCommandStrategy(Round currentRound, string pubkey, Timestamp currentBlockTime,
                AElfConsensusBehaviour consensusBehaviour) : base(currentRound, pubkey, currentBlockTime)
            {
                _consensusBehaviour = consensusBehaviour;
            }

            public override ConsensusCommand GetAEDPoSConsensusCommand()
            {
                var miningInterval = MiningInterval;
                var offset =
                    _consensusBehaviour == AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue && Order == 1
                        ? miningInterval
                        : Order.Add(MinersCount).Sub(1).Mul(miningInterval);
                var arrangedMiningTime = MiningTimeArrangingService.ArrangeMiningTimeBasedOnOffset(CurrentBlockTime, offset);
                return new ConsensusCommand
                {
                    Hint = new AElfConsensusHint {Behaviour = _consensusBehaviour}.ToByteString(),
                    ArrangedMiningTime = arrangedMiningTime,
                    MiningDueTime = arrangedMiningTime.AddMilliseconds(miningInterval),
                    LimitMillisecondsOfMiningBlock = DefaultBlockMiningLimit
                };
            }
        }
    }
}