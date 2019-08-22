using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.TestContract.LuckGame
{
    public partial class LuckGameContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        public Int32State RoundNumber { get; set; }
        public MappedState<long, int> LuckNumberRecords { get; set; }
        public MappedState<long, Address, Hash> UserPlayRecordId { get; set; }
        public MappedState<Hash, BetRecord> UserBetRecord { get; set; }
        public MappedState<Address, BetRecords> UserBetRecords { get; set; }
        public MappedState<long, BetInfos> RewardHistory { get; set; }
    }
}