using AElf.Standards.ACS4;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        /// <summary>
        /// Basically provides some useful fields for other strategies.
        /// </summary>
        public abstract class CommandStrategyBase : ICommandStrategy
        {
            protected readonly Round CurrentRound;
            protected readonly string Pubkey;
            protected readonly Timestamp CurrentBlockTime;

            /// <summary>
            /// In AElf Main Chain, miner will produce 8 blocks (as fast as possible) during every time slot by default.
            /// </summary>
            private const int TinyBlocksCount = 8;
            /// <summary>
            /// The minimum interval between two blocks of same time slot.
            /// </summary>
            protected const int TinyBlockMinimumInterval = 50;

            protected MinerInRound MinerInRound => CurrentRound.RealTimeMinersInformation[Pubkey];
            protected int Order => CurrentRound.GetMiningOrder(Pubkey);
            protected int MiningInterval => CurrentRound.GetMiningInterval();
            /// <summary>
            /// Producing time of every (tiny) block at most.
            /// </summary>
            private int TinyBlockSlotInterval => MiningInterval.Div(TinyBlocksCount);
            protected int MinersCount => CurrentRound.RealTimeMinersInformation.Count;
            /// <summary>
            /// Give 3/5 of producing time for mining by default.
            /// </summary>
            protected int DefaultBlockMiningLimit => TinyBlockSlotInterval.Mul(3).Div(5);
            /// <summary>
            /// If this tiny block is the last one of current time slot, give half of producing time for mining.
            /// </summary>
            protected int LastTinyBlockMiningLimit => TinyBlockSlotInterval.Div(2);
            /// <summary>
            /// If this block is of consensus behaviour NEXT_TERM, the producing time is MiningInterval,
            /// so the limitation of mining is 8 times than DefaultBlockMiningLimit.
            /// </summary>
            protected int LastBlockOfCurrentTermMiningLimit => MiningInterval.Mul(3).Div(5);

            protected CommandStrategyBase(Round currentRound, string pubkey, Timestamp currentBlockTime)
            {
                CurrentRound = currentRound;
                Pubkey = pubkey;
                CurrentBlockTime = currentBlockTime;
            }

            public ConsensusCommand GetConsensusCommand()
            {
                return GetAEDPoSConsensusCommand();
            }

            // ReSharper disable once InconsistentNaming
            public virtual ConsensusCommand GetAEDPoSConsensusCommand()
            {
                return ConsensusCommandProvider.InvalidConsensusCommand;
            }
        }
    }
}