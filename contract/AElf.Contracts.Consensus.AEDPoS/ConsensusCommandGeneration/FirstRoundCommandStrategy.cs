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

            public override ConsensusCommand GetConsensusCommand()
            {
                var miningInterval = MiningInterval;
                if (_consensusBehaviour == AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue)
                {
                    if (Order == 1)
                    {
                        // The boot miner can produce block immediately.
                        return new ConsensusCommand
                        {
                            Hint = new AElfConsensusHint {Behaviour = _consensusBehaviour}.ToByteString(),
                            ArrangedMiningTime = CurrentBlockTime.AddMilliseconds(miningInterval),
                            MiningDueTime = CurrentBlockTime.AddMilliseconds(miningInterval)
                                .AddMilliseconds(miningInterval),
                            LimitMillisecondsOfMiningBlock = DefaultBlockMiningLimit
                        };
                    }
                }

                var offset = Order.Add(MinersCount).Sub(1).Mul(miningInterval);
                return new ConsensusCommand
                {
                    Hint = new AElfConsensusHint {Behaviour = _consensusBehaviour}.ToByteString(),
                    ArrangedMiningTime = CurrentBlockTime.AddMilliseconds(offset),
                    MiningDueTime = CurrentBlockTime.AddMilliseconds(offset).AddMilliseconds(miningInterval),
                    LimitMillisecondsOfMiningBlock = DefaultBlockMiningLimit
                };
            }
        }
    }
}