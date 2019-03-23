using System;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Authorization
{
    public class ConsensusContractReferenceState : ContractReferenceState
    {
        internal MethodReference<Empty,SInt64Value> GetCurrentRoundNumber { get; set; }
        internal MethodReference<SInt64Value, Round> GetRoundInformation { get; set; }
    }

    public class AuthorizationContractState : ContractState
    {
        public ConsensusContractReferenceState ConsensusContract { get; set; }
        public MappedState<Address, Kernel.Authorization> MultiSig { get; set; }
        public MappedState<Hash, Proposal> Proposals { get; set; }
        public MappedState<Hash, Approved> Approved { get; set; }
    }
}