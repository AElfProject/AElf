using Acs1;
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
        
        public SingletonState<AuthorityInfo> CrossChainIndexingController { get; set; }

        public SingletonState<AuthorityInfo> SideChainLifetimeController { get; set; }
        
        public MappedState<string, MethodFees> TransactionFees { get; set; }
        
        public SingletonState<CrossChainIndexingProposal> CrossChainIndexingProposal { get; set; }
        
        public MappedState<Address, long> BannedMinerHeight { get; set; }
        
        public MappedState<Address, SideChainCreationRequestState> ProposedSideChainCreationRequestState { get; set; }

        public MappedState<int, SideChainCreationRequest> AcceptedSideChainCreationRequest { get; set; }


        public SingletonState<AuthorityInfo> MethodFeeController { get; set; }
        
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
        public MappedState<long, Hash> ParentChainTransactionStatusMerkleTreeRoot { get; set; }
        
        public SingletonState<IndexedParentChainBlockData> LastIndexedParentChainBlockData { get; set; }
            
        #endregion

        public SingletonState<long> LatestExecutedHeight { get; set; }
    }
}