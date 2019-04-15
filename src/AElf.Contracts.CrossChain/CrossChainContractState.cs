using AElf.CrossChain;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChain
{
    public partial class CrossChainContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        //public AuthorizationContractReferenceState AuthorizationContract { get; set; }
        public SingletonState<Hash> ConsensusContractSystemName { get; set; }
        public SingletonState<Hash> TokenContractSystemName { get; set; }
        public Int64State SideChainSerialNumber { get; set; }
        
        public MappedState<long, CrossChainBlockData> IndexedCrossChainBlockData { get; set; }
        
        public StringState NativeTokenSymbol { get; set; }

        #region side chain

        public MappedState<int, SideChainInfo> SideChainInfos { get; set; }
        public MappedState<int, long> CurrentSideChainHeight { get; set; }
        
        internal MappedState<int, BytesValue> SideChainInitialConsensusInfo { get; set; }
        public MappedState<int, long> IndexingBalance { get; set; }

        #endregion

        #region parent chain 

        public MappedState<long, Hash> TransactionMerkleTreeRootRecordedInParentChain { get; set; }
        public MappedState<long, long> ChildHeightToParentChainHeight { get; set; }
        public MappedState<long, MerklePath> TxRootMerklePathInParentChain { get; set; }
        public Int64State CurrentParentChainHeight { get; set; }
        public Int32State ParentChainId { get; set; }
        public MappedState<long, Hash> ParentChainTransactionStatusMerkleTreeRoot { get; set; }
        #endregion
    }
}