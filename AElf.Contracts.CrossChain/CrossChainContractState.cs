using System;
using AElf.Common;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.CrossChain
{
    public class AuthorizationContractReferenceState : ContractReferenceState
    {
        public Action<Proposal> Propose { get; set; }
        public Func<Address, Authorization> GetAuthorization { get; set; }
    }

    public class TokenContractReferenceState : ContractReferenceState
    {
        public Action<Address, ulong> Transfer { get; set; }
        public Action<Address, Address, ulong> TransferFrom { get; set; }
    }

    public class ConsensusContractReferenceState : ContractReferenceState
    {
        public Func<ulong> GetCurrentRoundNumber { get; set; }
        public Func<ulong, Round> GetRoundInfo { get; set; }
    }

    public class CrossChainContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        public AuthorizationContractReferenceState AuthorizationContract { get; set; }
        public TokenContractReferenceState TokenContract { get; set; }
        public ConsensusContractReferenceState ConsensusContract { get; set; }
        public UInt64State SideChainSerialNumber { get; set; }
        
        public MappedState<long, CrossChainBlockData> IndexedCrossChainBlockData { get; set; }

        #region side chain

        public MappedState<int, SideChainInfo> SideChainInfos { get; set; }
        public MappedState<int, long> CurrentSideChainHeight { get; set; }
        public MappedState<int, ulong> IndexingBalance { get; set; }

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