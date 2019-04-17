using AElf.Kernel;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContractState : ContractState
    {
        public BoolState Initialized { get; set; }

        public MappedState<Hash, ProfitItem> ProfitItemsMap { get; set; }

        public MappedState<Address, long> PeriodWeightsMap { get; set; }

        public MappedState<Hash, Address, ProfitDetails> ProfitDetailsMap { get; set; }
    }
}