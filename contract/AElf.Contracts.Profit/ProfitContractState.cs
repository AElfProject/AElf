using Acs1;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContractState : ContractState
    {
        public MappedState<Hash, Scheme> SchemeInfos { get; set; }

        public MappedState<Address, DistributedProfitsInfo> ReleasedProfitsMap { get; set; }

        public MappedState<Hash, Address, ProfitDetails> ProfitDetailsMap { get; set; }

        public MappedState<Address, CreatedSchemeIds> CreatedSchemeIds { get; set; }
        
        public MappedState<string, TokenAmounts> TransactionFees { get; set; }
    }
}