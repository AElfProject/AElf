using AElf.Kernel;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContractState : ContractState
    {
        public BoolState Initialized { get; set; }

        public MappedState<Hash, ProfitItem> ProfitItemsMap { get; set; }

        public MappedState<Hash, Address, long> WeightsMap { get; set; }
    }
}