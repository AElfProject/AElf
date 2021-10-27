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
        /// <summary>
        /// Why implement a strategy only for first round?
        /// Because during the first round, the ExpectedMiningTimes of every miner
        /// depends on the StartTimestamp configured before starting current blockchain,
        /// (which AElf Main Chain use new Timestamp {Seconds = 0},)
        /// thus we can't really give mining scheduler these data.
        /// The ActualMiningTimes will based on Orders of these miners.
        /// </summary>
        private class FirstRoundCommandStrategy : CommandStrategyBase
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
                    _consensusBehaviour == AElfConsensusBehaviour.UpdateValue && Order == 1
                        ? miningInterval
                        : Order.Add(MinersCount).Sub(1).Mul(miningInterval);
                var arrangedMiningTime =
                    MiningTimeArrangingService.ArrangeMiningTimeWithOffset(CurrentBlockTime, offset);
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