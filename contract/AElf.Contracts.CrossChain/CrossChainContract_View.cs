using AElf.Standards.ACS3;
using AElf.Standards.ACS7;
using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChain
{
    // ReSharper disable once PossibleNullReferenceException

    public partial class CrossChainContract
    {
        public override IndexedSideChainBlockData GetIndexedSideChainBlockDataByHeight(Int64Value input)
        {
            var indexedSideChainBlockData = State.IndexedSideChainBlockData[input.Value];
            return indexedSideChainBlockData ?? new IndexedSideChainBlockData();
        }

        public override CrossChainMerkleProofContext GetBoundParentChainHeightAndMerklePathByHeight(Int64Value input)
        {
            var boundParentChainHeight = State.ChildHeightToParentChainHeight[input.Value];
            Assert(boundParentChainHeight != 0);
            var merklePath = State.TxRootMerklePathInParentChain[input.Value];
            Assert(merklePath != null);
            return new CrossChainMerkleProofContext
            {
                MerklePathFromParentChain = merklePath,
                BoundParentChainHeight = boundParentChainHeight
            };
        }

        /// <summary>
        /// Cross chain txn verification.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override BoolValue VerifyTransaction(VerifyTransactionInput input)
        {
            var parentChainHeight = input.ParentChainHeight;
            var merkleTreeRoot = GetMerkleTreeRoot(input.VerifiedChainId, parentChainHeight);
            Assert(merkleTreeRoot != null,
                $"Parent chain block at height {parentChainHeight} is not recorded.");
            var rootCalculated = ComputeRootWithTransactionStatusMerklePath(input.TransactionId, input.Path);

            return new BoolValue {Value = merkleTreeRoot == rootCalculated};
        }

        public override GetChainStatusOutput GetChainStatus(Int32Value input)
        {
            var info = State.SideChainInfo[input.Value];
            Assert(info != null, "Side chain not found.");
            return new GetChainStatusOutput {Status = info.SideChainStatus};
        }

        public override Int64Value GetSideChainHeight(Int32Value input)
        {
            var info = State.SideChainInfo[input.Value];
            Assert(info != null, "Side chain not found.");
            var height = State.CurrentSideChainHeight[input.Value];
            return new Int64Value() {Value = height};
        }

        public override Int64Value GetParentChainHeight(Empty input)
        {
            var parentChainId = State.ParentChainId.Value;
            Assert(parentChainId != 0, "Parent chain not exist.");
            var parentChainHeight = State.CurrentParentChainHeight.Value;
            return new Int64Value
            {
                Value = parentChainHeight
            };
        }

        public override Int32Value GetParentChainId(Empty input)
        {
            var parentChainId = State.ParentChainId.Value;
            Assert(parentChainId != 0, "Parent chain not exist.");
            return new Int32Value() {Value = parentChainId};
        }

        public override Int64Value GetSideChainBalance(Int32Value input)
        {
            var chainId = input.Value;
            var sideChainInfo = State.SideChainInfo[chainId];
            Assert(sideChainInfo != null, "Side chain not found.");
            return new Int64Value {Value = GetSideChainIndexingFeeDeposit(chainId)};
        }
        
        public override Int64Value GetSideChainIndexingFeeDebt(Int32Value input)
        {
            var chainId = input.Value;
            var sideChainInfo = State.SideChainInfo[chainId];
            Assert(sideChainInfo != null, "Side chain not found.");
            
            return new Int64Value
            {
                Value = sideChainInfo.ArrearsInfo.Values.Sum()
            };
        }

        public override ChainIdAndHeightDict GetSideChainIdAndHeight(Empty input)
        {
            var dict = new ChainIdAndHeightDict();
            var serialNumber = State.SideChainSerialNumber.Value;
            for (long i = 1; i <= serialNumber; i++)
            {
                int chainId = GetChainId(i);
                var sideChainInfo = State.SideChainInfo[chainId];
                if (sideChainInfo.SideChainStatus == SideChainStatus.Terminated)
                    continue;
                var height = State.CurrentSideChainHeight[chainId];
                dict.IdHeightDict.Add(chainId, height);
            }

            return dict;
        }

        public override ChainIdAndHeightDict GetAllChainsIdAndHeight(Empty input)
        {
            var dict = GetSideChainIdAndHeight(new Empty());

            if (State.ParentChainId.Value == 0)
                return dict;
            var parentChainHeight = GetParentChainHeight(new Empty()).Value;
            Assert(parentChainHeight > AElfConstants.GenesisBlockHeight, "Invalid parent chain height");
            dict.IdHeightDict.Add(State.ParentChainId.Value, parentChainHeight);
            return dict;
        }

        public override SideChainIndexingInformationList GetSideChainIndexingInformationList(Empty input)
        {
            var sideChainIndexingInformationList = new SideChainIndexingInformationList();
            var sideChainIdAndHeightDict = GetSideChainIdAndHeight(new Empty());
            foreach (var kv in sideChainIdAndHeightDict.IdHeightDict)
            {
                var chainId = kv.Key;
                sideChainIndexingInformationList.IndexingInformationList.Add(new SideChainIndexingInformation
                {
                    ChainId = chainId,
                    IndexedHeight = kv.Value
                });
            }

            return sideChainIndexingInformationList;
        }

        public override Address GetSideChainCreator(Int32Value input)
        {
            var info = State.SideChainInfo[input.Value];
            Assert(info != null, "Side chain not found.");
            return info.Proposer;
        }

        public override ChainInitializationData GetChainInitializationData(Int32Value input)
        {
            var res = State.SideChainInitializationData[input.Value];
            Assert(res!=null, "Side chain not found.");
            return res;
        }
        
        public override GetIndexingProposalStatusOutput GetIndexingProposalStatus(Empty input)
        {
            var res = new GetIndexingProposalStatusOutput();
            var pendingCrossChainIndexingProposal = State.IndexingPendingProposal.Value;
            if (pendingCrossChainIndexingProposal == null)
                return res;
            
            var crossChainIndexingController = GetCrossChainIndexingController();
            foreach (var chainIndexingProposal in pendingCrossChainIndexingProposal.ChainIndexingProposalCollections.Values)
            {
                var pendingChainIndexingProposalStatus = new PendingChainIndexingProposalStatus();
                var proposalInfo = Context.Call<ProposalOutput>(crossChainIndexingController.ContractAddress,
                    nameof(AuthorizationContractContainer.AuthorizationContractReferenceState.GetProposal),
                    chainIndexingProposal.ProposalId);
                pendingChainIndexingProposalStatus.Proposer = chainIndexingProposal.Proposer;
                pendingChainIndexingProposalStatus.ProposalId = chainIndexingProposal.ProposalId;
                pendingChainIndexingProposalStatus.ToBeReleased =
                    proposalInfo.ToBeReleased &&
                    proposalInfo.OrganizationAddress == crossChainIndexingController.OwnerAddress;
                pendingChainIndexingProposalStatus.ExpiredTime = proposalInfo.ExpiredTime;
                pendingChainIndexingProposalStatus.ProposedCrossChainBlockData = chainIndexingProposal.ProposedCrossChainBlockData;
                res.ChainIndexingProposalStatus[chainIndexingProposal.ChainId] = pendingChainIndexingProposalStatus;
            }
            
            return res;
        }

        public override Int64Value GetSideChainIndexingFeePrice(Int32Value input)
        {
            var sideChainInfo = State.SideChainInfo[input.Value];
            Assert(sideChainInfo != null, "Side chain not found.");
            return new Int64Value
            {
                Value = sideChainInfo.IndexingPrice
            };
        }

        public override AuthorityInfo GetCrossChainIndexingController(Empty input)
        {
            return GetCrossChainIndexingController();
        }

        public override AuthorityInfo GetSideChainLifetimeController(Empty input)
        {
            return GetSideChainLifetimeController();
        }

        public override AuthorityInfo GetSideChainIndexingFeeController(Int32Value input)
        {
            var sideChainInfo = State.SideChainInfo[input.Value];
            Assert(sideChainInfo != null, "Side chain not found.");
            return sideChainInfo.IndexingFeeController;
        }
    }
}