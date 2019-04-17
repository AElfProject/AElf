using AElf.Contracts.ProposalContract;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Proposal
{
    public class ProposalContractState : ContractState
    {
        public MappedState<Hash, ProposalInfo> Proposals { get; set; }
        public MappedState<Hash, ApprovedResult> Approved { get; set; }
    }
}