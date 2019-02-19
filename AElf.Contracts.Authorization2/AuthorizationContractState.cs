using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Authorization2
{
    public class ConsensusContractReferenceState : ContractReferenceState
    {
        public Func<ulong> GetCurrentRoundNumber { get; set; }
        public Func<ulong, Round> GetRoundInfo { get; set; }
    }

    public class AuthorizationContractState : ContractState
    {
        public ConsensusContractReferenceState ConsensusContract { get; set; }
        public MappedState<Address, Authorization> MultiSig { get; set; }
        public MappedState<Hash, Proposal> Proposals { get; set; }
        public MappedState<Hash, Approved> Approved { get; set; }
    }
}