using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.ProposalContract;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.ParliamentAuth
{
    public class ParliamentAuthState : ContractState
    {
        public MappedState<Address, Organization> Organisations { get; set; }

        public BoolState Initialized { get; set; }
        
        public SingletonState<Hash> ConsensusContractSystemName { get; set; }
        internal ConsensusContractContainer.ConsensusContractReferenceState ConsensusContract {get; set; }
        public SingletonState<Hash> ProposalContractSystemName { get; set; }
        public MappedState<Hash, BoolValue> ProposalReleaseStatus { get; set; }
        internal ProposalContractContainer.ProposalContractReferenceState ProposalContract { get; set; }        
        internal BasicContractZeroContainer.BasicContractZeroReferenceState BasicContractZero { get; set; }
    }
}