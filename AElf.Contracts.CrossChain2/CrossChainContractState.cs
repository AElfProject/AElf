using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.CrossChain2
{
    public class CrossChainContractState : ContractState
    {
        public UInt64State SideChainSerialNumber { get; set; }

        #region side chain

        public MappedState<Hash, SideChainInfo> SideChainInfos { get; set; }
        public MappedState<Hash, ulong> SideChainHeight { get; set; }
        public MappedState<ulong, IndexedSideChainBlockDataResult> IndexedSideChainBlockInfoResult { get; set; }
        public MappedState<Hash, ulong> IndexingBalance { get; set; }

        #endregion

        #region parent chain 

        public MappedState<ulong, ParentChainBlockData> ParentChainBlockInfo { get; set; }
        public MappedState<ulong, ulong> ChildHeightToParentChainHeight { get; set; }
        public MappedState<ulong, MerklePath> TxRootMerklePathInParentChain { get; set; }
        public UInt64State CurrentParentChainHeight { get; set; }
        public UInt64State RecordedBlockHeight { get; set; }

        #endregion
    }
}