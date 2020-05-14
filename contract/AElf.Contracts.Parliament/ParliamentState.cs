using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Parliament
{
    public class ParliamentState : ContractState
    {
        public MappedState<Address, Organization> Organizations { get; set; }

        public BoolState Initialized { get; set; }

        public SingletonState<Address> DefaultOrganizationAddress { get; set; }

        internal AEDPoSContractContainer.AEDPoSContractReferenceState ConsensusContract { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        public MappedState<Hash, ProposalInfo> Proposals { get; set; }
        public MappedState<string, MethodFees> TransactionFees { get; set; }

        public SingletonState<ProposerWhiteList> ProposerWhiteList { get; set; }
        public SingletonState<AuthorityInfo> MethodFeeController { get; set; }
    }
}