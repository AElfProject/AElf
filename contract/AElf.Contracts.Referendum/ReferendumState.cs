using Acs1;
using Acs3;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Referendum
{
    public class ReferendumState : ContractState
    {
        public BoolState Initialized { get; set; }
        public MappedState<Address, Hash, Receipt> LockedTokenAmount { get; set; }
        public MappedState<Address, Organization> Organizations { get; set; }
        public MappedState<Hash, ProposalInfo> Proposals { get; set; }
        public MappedState<string, MethodFees> TransactionFees { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal ParliamentContractContainer.ParliamentContractReferenceState ParliamentContract { get; set; }
        public SingletonState<AuthorityInfo> MethodFeeController { get; set; }
    }
}