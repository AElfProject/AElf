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
        public class NormalBlockCommandStrategy : CommandStrategyBase
        {
            public NormalBlockCommandStrategy(Round currentRound, string pubkey, Timestamp currentBlockTime) : base(
                currentRound, pubkey, currentBlockTime)
            {
            }

            public override ConsensusCommand GetAEDPoSConsensusCommand()
            {
                var arrangedMiningTime = CurrentRound.GetExpectedMiningTime(Pubkey);
                var miningDueTime = arrangedMiningTime.AddMilliseconds(MiningInterval);

                return new ConsensusCommand
                {
                    Hint = new AElfConsensusHint
                    {
                        Behaviour = CurrentRound.IsMinerListJustChanged
                            ? AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue
                            : AElfConsensusBehaviour.UpdateValue
                    }.ToByteString(),
                    ArrangedMiningTime = arrangedMiningTime,
                    MiningDueTime = miningDueTime,
                    LimitMillisecondsOfMiningBlock = DefaultBlockMiningLimit
                };
            }
        }
    }
}