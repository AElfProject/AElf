using System;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChain
{
    public partial class CrossChainContract
    {
        public class AuthorizationContractReferenceState : ContractReferenceState
        {
            internal MethodReference<Proposal,Hash> Propose { get; set; }
            internal MethodReference<Address, Kernel.Authorization> GetAuthorization { get; set; }
        }

        public class TokenContractReferenceState : ContractReferenceState
        {
            internal MethodReference<TransferInput,Empty> Transfer { get; set; }
            internal MethodReference<TransferFromInput,Empty> TransferFrom { get; set; }
        
            internal MethodReference<GetBalanceInput, GetBalanceOutput> GetBalance { get; set; }
        }

        public class ConsensusContractReferenceState : ContractReferenceState
        {
            public MethodReference<DPoSInformation, Empty> UpdateMainChainConsensus { get; set; }
            public MethodReference<Empty, Miners> GetCurrentMiners { get; set; }
        }
        
        public class BasicContractZeroReferenceState : ContractReferenceState
        {
            internal MethodReference<Hash, Address> GetContractAddressByName { get; set; }
        }
    }

    public partial class CrossChainContractState
    {
        public CrossChainContract.TokenContractReferenceState TokenContract { get; set; }
        public CrossChainContract.ConsensusContractReferenceState ConsensusContract { get; set; }
        public CrossChainContract.BasicContractZeroReferenceState BasicContractZero { get; set; }
    }
}