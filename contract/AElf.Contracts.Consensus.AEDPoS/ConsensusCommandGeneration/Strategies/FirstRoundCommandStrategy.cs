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
                if (_consensusBehaviour == AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue)
                {
                    if (Order == 1)
                    {
                        // The boot miner can produce block immediately.
                        var arrangedMiningTime = CurrentBlockTime.AddMilliseconds(miningInterval);
                        var miningDueTime = arrangedMiningTime.AddMilliseconds(miningInterval);
                        return new ConsensusCommand
                        {
                            Hint = new AElfConsensusHint {Behaviour = _consensusBehaviour}.ToByteString(),
                            ArrangedMiningTime = arrangedMiningTime,
                            MiningDueTime = miningDueTime,
                            LimitMillisecondsOfMiningBlock = DefaultBlockMiningLimit
                        };
                    }
                }

                {
                    var offset = Order.Add(MinersCount).Sub(1).Mul(miningInterval);
                    var arrangedMiningTime = CurrentBlockTime.AddMilliseconds(offset);
                    var miningDueTime = arrangedMiningTime.AddMilliseconds(miningInterval);
                    return new ConsensusCommand
                    {
                        Hint = new AElfConsensusHint {Behaviour = _consensusBehaviour}.ToByteString(),
                        ArrangedMiningTime = arrangedMiningTime,
                        MiningDueTime = miningDueTime,
                        LimitMillisecondsOfMiningBlock = DefaultBlockMiningLimit
                    };
                }
            }
        }
    }
}