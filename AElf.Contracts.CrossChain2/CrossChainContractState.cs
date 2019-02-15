using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.CrossChain2
{
    public class AuthorizationContractReferenceState : ContractReferenceState
    {
        public Action<Proposal> Propose { get; set; }
    }

    public class TokenContractReferenceState : ContractReferenceState
    {
        public Action<Address, ulong> Lock { get; set; }
        public Action<Address, ulong> Unlock { get; set; }
    }

    public class CrossChainContractState : ContractState
    {
        public AuthorizationContractReferenceState AuthorizationContract { get; set; }
        public TokenContractReferenceState TokenContract { get; set; }
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