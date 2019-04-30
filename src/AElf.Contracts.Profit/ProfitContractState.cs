using AElf.Kernel;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContractState : ContractState
    {
        public BoolState Initialized { get; set; }

        public MappedState<Hash, ProfitItem> ProfitItemsMap { get; set; }

        public MappedState<Address, ReleasedProfitsInformation> ReleasedProfitsMap { get; set; }

        public MappedState<Hash, Address, ProfitDetails> ProfitDetailsMap { get; set; }

        public MappedState<Address, CreatedProfitItems> CreatedProfitItemsMap { get; set; }
    }
}