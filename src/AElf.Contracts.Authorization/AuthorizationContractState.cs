using AElf.Contracts.Consensus.DPoS;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
namespace AElf.Contracts.Authorization
{
    public class AuthorizationContractState : ContractState
    {
        internal ConsensusContractContainer.ConsensusContractReferenceState ConsensusContract { get; set; }
        public MappedState<Address, Kernel.Authorization> MultiSig { get; set; }
        public MappedState<Hash, Proposal> Proposals { get; set; }
        public MappedState<Hash, Approved> Approved { get; set; }
    }
}