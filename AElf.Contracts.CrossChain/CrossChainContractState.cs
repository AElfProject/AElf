using System;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.MultiToken.Messages;
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
        public Action<TransferInput> Transfer { get; set; }
        public Action<TransferFromInput> TransferFrom { get; set; }
        
        public Func<GetBalanceInput, GetBalanceOutput> GetBalance { get; set; }
    }

    public class ConsensusContractReferenceState : ContractReferenceState
    {
        public Action<byte[]> UpdateMainChainConsensus { get; set; }
        
        public Func<Miners> GetCurrentMiners { get; set; }
        public Func<long> GetCurrentRoundNumber { get; set; }
        public Func<long, Round> GetRoundInformation { get; set; }
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