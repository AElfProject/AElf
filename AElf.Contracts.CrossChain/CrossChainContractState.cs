using System;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.MultiToken.Messages;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChain
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
    }

    public class CrossChainContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        //public AuthorizationContractReferenceState AuthorizationContract { get; set; }
        public TokenContractReferenceState TokenContract { get; set; }
        public ConsensusContractReferenceState ConsensusContract { get; set; }
        public Int64State SideChainSerialNumber { get; set; }
        
        public MappedState<long, CrossChainBlockData> IndexedCrossChainBlockData { get; set; }

        #region side chain

        public MappedState<int, SideChainInfo> SideChainInfos { get; set; }
        public MappedState<int, long> CurrentSideChainHeight { get; set; }
        
        public MappedState<int, Miners> SideChainInitialConsensuseInfo { get; set; }
        public MappedState<int, long> IndexingBalance { get; set; }

        #endregion

        #region parent chain 

        public MappedState<long, Hash> TransactionMerkleTreeRootRecordedInParentChain { get; set; }
        public MappedState<long, long> ChildHeightToParentChainHeight { get; set; }
        public MappedState<long, MerklePath> TxRootMerklePathInParentChain { get; set; }
        public Int64State CurrentParentChainHeight { get; set; }
        public Int32State ParentChainId { get; set; }
        #endregion
    }
}