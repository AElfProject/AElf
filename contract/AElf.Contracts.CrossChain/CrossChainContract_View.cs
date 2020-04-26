using Acs1;
using Acs3;
using Acs7;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChain
{
    // ReSharper disable once PossibleNullReferenceException

    public partial class CrossChainContract
    {
        public override CrossChainBlockData GetIndexedCrossChainBlockDataByHeight(Int64Value input)
        {
            var crossChainBlockData = new CrossChainBlockData();
            var indexedParentChainBlockData = State.LastIndexedParentChainBlockData.Value;
            if (indexedParentChainBlockData != null && indexedParentChainBlockData.LocalChainHeight == input.Value)
                crossChainBlockData.ParentChainBlockDataList.AddRange(indexedParentChainBlockData
                    .ParentChainBlockDataList);

            var indexedSideChainBlockData = GetIndexedSideChainBlockDataByHeight(input);
            crossChainBlockData.SideChainBlockDataList.AddRange(indexedSideChainBlockData.SideChainBlockDataList);
            return crossChainBlockData;
        }

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

            //Api.Assert((parentRoot??Hash.Empty).Equals(rootCalculated), "Transaction verification Failed");
            return new BoolValue {Value = merkleTreeRoot.Equals(rootCalculated)};
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
            var parentChainHeight = State.CurrentParentChainHeight.Value;
            return new Int64Value
            {
                Value = parentChainHeight
            };
        }

        public override Int32Value GetParentChainId(Empty input)
        {
            var parentChainId = State.ParentChainId.Value;
            Assert(parentChainId != 0);
            return new Int32Value() {Value = parentChainId};
        }

        public override Int64Value GetSideChainBalance(Int32Value input)
        {
            var chainId = input.Value;
            var sideChainInfo = State.SideChainInfo[chainId];
            Assert(sideChainInfo != null, "Side chain not found.");
            return new Int64Value {Value = State.IndexingBalance[chainId]};
        }

        public override SideChainIdAndHeightDict GetSideChainIdAndHeight(Empty input)
        {
            var dict = new SideChainIdAndHeightDict();
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

        public override SideChainIdAndHeightDict GetAllChainsIdAndHeight(Empty input)
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
            var sideChainInfo = State.SideChainInfo[input.Value];
            var sideChainCreationRequest = State.AcceptedSideChainCreationRequest[input.Value];

            Assert(sideChainInfo != null && sideChainCreationRequest != null, "Side chain not found.");

            SetContractStateRequired(State.TokenContract, SmartContractConstants.TokenContractSystemName);
            var res = new ChainInitializationData
            {
                CreationHeightOnParentChain = sideChainInfo.CreationHeightOnParentChain,
                ChainId = input.Value,
                Creator = sideChainInfo.Proposer,
                CreationTimestamp = sideChainInfo.CreationTimestamp,
                ChainCreatorPrivilegePreserved = sideChainInfo.IsPrivilegePreserved,
                ParentChainTokenContractAddress = State.TokenContract.Value
            };
            ByteString consensusInformation = State.SideChainInitialConsensusInfo[input.Value].Value;
            res.ChainInitializationConsensusInfo = new ChainInitializationConsensusInfo
                {InitialMinerListData = consensusInformation};

            ByteString nativeTokenInformation = GetNativeTokenInfo().ToByteString();
            res.NativeTokenInfoData = nativeTokenInformation;

            ByteString resourceTokenInformation = GetResourceTokenInfo().ToByteString();
            res.ResourceTokenInfo = new ResourceTokenInfo
            {
                ResourceTokenListData = resourceTokenInformation,
                InitialResourceAmount = {sideChainCreationRequest.InitialResourceAmount}
            };
            
            if (sideChainCreationRequest.IsPrivilegePreserved)
            {
                ByteString sideChainTokenInformation =
                    GetTokenInfo(sideChainCreationRequest.SideChainTokenSymbol).ToByteString();
                res.ChainPrimaryTokenInfo = new ChainPrimaryTokenInfo
                {
                    ChainPrimaryTokenData = sideChainTokenInformation,
                    SideChainTokenInitialIssueList = {sideChainCreationRequest.SideChainTokenInitialIssueList},
                };
            }

            return res;
        }

        public override GetPendingCrossChainIndexingProposalOutput GetPendingCrossChainIndexingProposal(Empty input)
        {
            var res = new GetPendingCrossChainIndexingProposalOutput();
            var exists = TryGetProposalWithStatus(CrossChainIndexingProposalStatus.Pending,
                out var pendingCrossChainIndexingProposal);
            Assert(exists, "Cross chain indexing with Pending status not found.");
            var crossChainIndexingController = GetCrossChainIndexingController();

            res.Proposer = pendingCrossChainIndexingProposal.Proposer;
            res.ProposalId = pendingCrossChainIndexingProposal.ProposalId;
            var proposalInfo = Context.Call<ProposalOutput>(crossChainIndexingController.ContractAddress,
                nameof(AuthorizationContractContainer.AuthorizationContractReferenceState.GetProposal),
                pendingCrossChainIndexingProposal.ProposalId);

            res.ToBeReleased = proposalInfo.ToBeReleased &&
                               proposalInfo.OrganizationAddress == crossChainIndexingController.OwnerAddress;
            res.ExpiredTime = proposalInfo.ExpiredTime;
            res.ProposedCrossChainBlockData = pendingCrossChainIndexingProposal.ProposedCrossChainBlockData;
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
            return sideChainInfo.IndexingFeeController;
        }
    }
}