using Acs1;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.ReferendumAuth
{
    public class ReferendumAuthState : ContractState
    {
        public MappedState<Address, Hash, Receipt> LockedTokenAmount { get; set; }
        public MappedState<Hash, long> ApprovedTokenAmount { get; set; }
        public MappedState<Address, Organization> Organisations { get; set; }
        public MappedState<Hash, ProposalInfo> Proposals { get; set; }
        public MappedState<string, TokenAmounts> TransactionFees { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal ParliamentAuthContractContainer.ParliamentAuthContractReferenceState ParliamentAuthContract { get; set; }
    }
}