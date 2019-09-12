using Acs4;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        public abstract class CommandStrategyBase : ICommandStrategy
        {
            protected readonly Round CurrentRound;
            protected readonly string Pubkey;
            protected readonly Timestamp CurrentBlockTime;

            protected const int TinyBlocksCount = 8;

            protected int Order => CurrentRound.GetMiningOrder(Pubkey);
            protected int MiningInterval => CurrentRound.GetMiningInterval();
            protected int TinyBlockSlotInterval => MiningInterval.Div(TinyBlocksCount);
            protected int MinersCount => CurrentRound.RealTimeMinersInformation.Count;
            protected int DefaultBlockMiningLimit => TinyBlockSlotInterval.Mul(3).Div(5);
            protected int LastTinyBlockMiningLimit => TinyBlockSlotInterval.Div(2);
            protected int LastBlockOfCurrentTermMiningLimit => TinyBlockSlotInterval.Mul(3).Div(5);

            public CommandStrategyBase(Round currentRound, string pubkey, Timestamp currentBlockTime)
            {
                CurrentRound = currentRound;
                Pubkey = pubkey;
                CurrentBlockTime = currentBlockTime;
            }

            public virtual ConsensusCommand GetConsensusCommand()
            {
                return ConsensusCommandProvider.InvalidConsensusCommand;
            }
        }
    }
}