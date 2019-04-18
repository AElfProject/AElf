using AElf.Kernel;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContractState : ContractState
    {
        public BoolState Initialized { get; set; }

        public MappedState<Hash, ProfitItem> ProfitItemsMap { get; set; }

        public MappedState<Address, long> PeriodWeightsMap { get; set; }

        // TODO: Remove.
        /// <summary>
        /// id of profit item 1 -> id of profit item 2 -> is profit item 2 sharing profit item 1's profits.
        /// </summary>
        public MappedState<Hash, Hash, bool> RegisterMap { get; set; }

        public MappedState<Hash, Address, ProfitDetails> ProfitDetailsMap { get; set; }
    }
}