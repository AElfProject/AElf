using AElf.Sdk.CSharp.State;
using AElf.Types;
using Acs1;

namespace AElf.Contracts.AssociationAuth
{
    public partial class AssociationAuthState : ContractState
    {
        public MappedState<Address, Organization> Organisations { get; set; }
        public MappedState<Hash, ProposalInfo> Proposals { get; set; }
        public MappedState<string, TokenAmounts> TransactionFees { get; set; }
    }
}