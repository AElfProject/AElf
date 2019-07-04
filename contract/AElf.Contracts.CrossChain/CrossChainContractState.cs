using Acs7;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChain
{
    public partial class CrossChainContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        public Int64State SideChainSerialNumber { get; set; }
        
        public SingletonState<Address> Owner { get; set; }
        
        #region side chain

        public MappedState<int, SideChainInfo> SideChainInfo { get; set; }
        public MappedState<int, long> CurrentSideChainHeight { get; set; }
        
        internal MappedState<int, BytesValue> SideChainInitialConsensusInfo { get; set; }
        public MappedState<int, long> IndexingBalance { get; set; }

        public MappedState<long, IndexedSideChainBlockData> IndexedSideChainBlockData { get; set; }
        
        #endregion

        #region parent chain 

        public MappedState<long, Hash> TransactionMerkleTreeRootRecordedInParentChain { get; set; }
        public MappedState<long, long> ChildHeightToParentChainHeight { get; set; }
        public MappedState<long, MerklePath> TxRootMerklePathInParentChain { get; set; }
        public Int64State CurrentParentChainHeight { get; set; }
        public Int32State ParentChainId { get; set; }
        public Int64State CreationHeightOnParentChain { get; set; }
        public MappedState<long, Hash> ParentChainTransactionStatusMerkleTreeRoot { get; set; }
        
        public SingletonState<IndexedParentChainBlockData> LastIndexedParentChainBlockData { get; set; }
            
        #endregion
    }
}