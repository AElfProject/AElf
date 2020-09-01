using AElf.Sdk.CSharp.State;
using AElf.Types;
using AElf.Standards.ACS1;

namespace AElf.Contracts.Association
{
    public partial class AssociationState : ContractState
    {
        public MappedState<Address, Organization> Organizations { get; set; }
        public MappedState<Hash, ProposalInfo> Proposals { get; set; }
        public MappedState<string, MethodFees> TransactionFees { get; set; }
        public SingletonState<AuthorityInfo> MethodFeeController { get; set; }
    }
}