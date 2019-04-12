using Acs3;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.ParliamentAuth
{
    public class ParliamentAuthState : ContractState
    {
        public SingletonState<Hash> ConsensusContractSystemName { get; set; }
        public MappedState<Hash, ProposalInfo> Proposals { get; set; }
        public MappedState<Hash, ApprovedResult> Approved { get; set; }

        public BoolState Initialized { get; set; }
        
        internal ConsensusContractContainer.ConsensusContractReferenceState ConsensusContractReferenceState {get; set; }
    }
}