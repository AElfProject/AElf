using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContractState : ContractState
    {
        public MappedState<Hash, ProfitItem> ProfitItemsMap { get; set; }

        public MappedState<Address, ReleasedProfitsInformation> ReleasedProfitsMap { get; set; }

        public MappedState<Hash, Address, ProfitDetails> ProfitDetailsMap { get; set; }

        public MappedState<Address, CreatedProfitIds> CreatedProfitIds { get; set; }
    }
}