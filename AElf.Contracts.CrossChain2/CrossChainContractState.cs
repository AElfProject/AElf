using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.CrossChain2
{
    public class AuthorizationContractReferenceState : ContractReferenceState
    {
        public Action<Proposal> Propose { get; set; }
        public Func<Address, Authorization> GetAuthorization { get; set; }
    }

    public class TokenContractReferenceState : ContractReferenceState
    {
        public Action<Address, ulong> Lock { get; set; }
        public Action<Address, ulong> Unlock { get; set; }
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

        #region side chain

        public MappedState<int, SideChainInfo> SideChainInfos { get; set; }
        public MappedState<int, ulong> CurrentSideChainHeight { get; set; }
        public MappedState<ulong, IndexedSideChainBlockDataResult> IndexedSideChainBlockInfoResult { get; set; }
        public MappedState<int, ulong> IndexingBalance { get; set; }

        #endregion

        #region parent chain 

        public MappedState<ulong, ParentChainBlockData> ParentChainBlockInfo { get; set; }
        public MappedState<ulong, ulong> ChildHeightToParentChainHeight { get; set; }
        public MappedState<ulong, MerklePath> TxRootMerklePathInParentChain { get; set; }
        public UInt64State CurrentParentChainHeight { get; set; }

        #endregion
    }
}