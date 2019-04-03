using System;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.Consensus.DPoS;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Authorization
{
    public class AuthorizationContractState : ContractState
    {
        internal ConsensusContractContainer.ConsensusContractReferenceState ConsensusContract { get; set; }
        public MappedState<Address, Kernel.Authorization> MultiSig { get; set; }
        public MappedState<Hash, ProposalInfo> Proposals { get; set; }
        
        public MappedState<Hash, SInt64Value> ReleasedBlockHeight { get; set; }
        public MappedState<Hash, Approved> Approved { get; set; }
        
        public SingletonState<Address> Genesis { get; set; }
        public SingletonState<Address> Director { get; set; }
        
        public SingletonState<Hash> ConsensusContractSystemName { get; set; }
        public SingletonState<Hash> TokenContractSystemName { get; set; }
        public BoolState Initialized { get; set; }
    }
}