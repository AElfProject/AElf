using Acs1;
using Acs3;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Parliament
{
    public class ParliamentState : ContractState
    {
        public MappedState<Address, Organization> Organisations { get; set; }

        public BoolState Initialized { get; set; }

        public SingletonState<Address> DefaultOrganizationAddress { get; set; }

        internal AEDPoSContractContainer.AEDPoSContractReferenceState ConsensusContract { get; set; }
        public MappedState<Hash, ProposalInfo> Proposals { get; set; }
        public MappedState<string, MethodFees> TransactionFees { get; set; }

        public SingletonState<ProposerWhiteList> ProposerWhiteList { get; set; }
    }
}