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
        public override CrossChainBlockData GetIndexedCrossChainBlockDataByHeight(SInt64Value input)
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

        public override IndexedSideChainBlockData GetIndexedSideChainBlockDataByHeight(SInt64Value input)
        {
            var indexedSideChainBlockData = State.IndexedSideChainBlockData[input.Value];
            return indexedSideChainBlockData ?? new IndexedSideChainBlockData();
        }

        public override CrossChainMerkleProofContext GetBoundParentChainHeightAndMerklePathByHeight(SInt64Value input)
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

        public override SInt32Value GetChainStatus(SInt32Value input)
        {
            var info = State.SideChainInfo[input.Value];
            Assert(info != null, "Side chain not found.");
            return new SInt32Value() {Value = (int) info.SideChainStatus};
        }

        public override SInt64Value GetSideChainHeight(SInt32Value input)
        {
            var info = State.SideChainInfo[input.Value];
            Assert(info != null, "Side chain not found.");
            var height = State.CurrentSideChainHeight[input.Value];
            return new SInt64Value() {Value = height};
        }

        public override SInt64Value GetParentChainHeight(Empty input)
        {
            var parentChainHeight = State.CurrentParentChainHeight.Value;
            return new SInt64Value
            {
                Value = parentChainHeight
            };
        }

        public override SInt32Value GetParentChainId(Empty input)
        {
            var parentChainId = State.ParentChainId.Value;
            Assert(parentChainId != 0);
            return new SInt32Value() {Value = parentChainId};
        }

        public override SInt64Value GetSideChainBalance(SInt32Value input)
        {
            var chainId = input.Value;
            var sideChainInfo = State.SideChainInfo[chainId];
            Assert(sideChainInfo != null, "Side chain not found.");
            return new SInt64Value {Value = State.IndexingBalance[chainId]};
        }

        public override SideChainIdAndHeightDict GetSideChainIdAndHeight(Empty input)
        {
            var dict = new SideChainIdAndHeightDict();
            var serialNumber = State.SideChainSerialNumber.Value;
            for (long i = 1; i <= serialNumber; i++)
            {
                int chainId = GetChainId(i);
                var sideChainInfo = State.SideChainInfo[chainId];
                if (sideChainInfo.SideChainStatus != SideChainStatus.Active)
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
            Assert(parentChainHeight > Constants.GenesisBlockHeight, "Invalid parent chain height");
            dict.IdHeightDict.Add(State.ParentChainId.Value, parentChainHeight);
            return dict;
        }

        public override SideChainIndexingInformationList GetSideChainIndexingInformationList(Empty input)
        {
            var sideChainIndexingInformationList = new SideChainIndexingInformationList();
            var sideChainIdAndHeightDict = GetSideChainIdAndHeight(new Empty());
            foreach (var kv in sideChainIdAndHeightDict.IdHeightDict)
            {
                int chainId = kv.Key;
                var balance = State.IndexingBalance[chainId];
                var sideChainInfo = State.SideChainInfo[chainId];
                var indexingPrice = sideChainInfo.SideChainCreationRequest.IndexingPrice;
                var toBeIndexedCount = indexingPrice == 0 ? long.MaxValue : balance.Div(indexingPrice);
                sideChainIndexingInformationList.IndexingInformationList.Add(new SideChainIndexingInformation
                {
                    ChainId = chainId,
                    IndexedHeight = kv.Value,
                    ToBeIndexedCount = toBeIndexedCount
                });
            }

            return sideChainIndexingInformationList;
        }

        public override Address GetSideChainCreator(SInt32Value input)
        {
            var info = State.SideChainInfo[input.Value];
            Assert(info != null, "Side chain not found.");
            return info.Proposer;
        }

        public override ChainInitializationData GetChainInitializationData(SInt32Value chainId)
        {
            var sideChainInfo = State.SideChainInfo[chainId.Value];
            Assert(sideChainInfo != null, "Side chain not found.");
            var res = new ChainInitializationData
            {
                CreationHeightOnParentChain = sideChainInfo.CreationHeightOnParentChain,
                ChainId = chainId.Value,
                Creator = sideChainInfo.Proposer,
                CreationTimestamp = sideChainInfo.CreationTimestamp,
                ChainCreatorPrivilegePreserved = sideChainInfo.SideChainCreationRequest.IsPrivilegePreserved,
                InitialResourceAmount = {sideChainInfo.SideChainCreationRequest.InitialResourceAmount},
                SideChainTokenInitialIssueList = {sideChainInfo.SideChainCreationRequest.SideChainTokenInitialIssueList}
            };
            ByteString consensusInformation = State.SideChainInitialConsensusInfo[chainId.Value].Value;
            res.ExtraInformation.Add(consensusInformation);

            ByteString nativeTokenInformation = GetNativeTokenInfo().ToByteString();
            res.ExtraInformation.Add(nativeTokenInformation);

            ByteString resourceTokenInformation = GetResourceTokenInfo().ToByteString();
            res.ExtraInformation.Add(resourceTokenInformation);

            ByteString sideChainTokenInformation =
                GetTokenInfo(sideChainInfo.SideChainCreationRequest.SideChainTokenSymbol)
                    .ToByteString();
            res.ExtraInformation.Add(sideChainTokenInformation);
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

        public override SInt64Value GetSideChainIndexingFeePrice(SInt32Value input)
        {
            var sideChainInfo = State.SideChainInfo[input.Value];
            Assert(sideChainInfo != null, "Side chain not found.");
            return new SInt64Value
            {
                Value = sideChainInfo.SideChainCreationRequest.IndexingPrice
            };
        }

        public override AuthorityStuff GetCrossChainIndexingController(Empty input)
        {
            return GetCrossChainIndexingController();
        }

        public override AuthorityStuff GetSideChainLifetimeController(Empty input)
        {
            return GetSideChainLifetimeController();
        }

        public override GetSideChainIndexingFeeControllerOutput GetSideChainIndexingFeeController(SInt32Value input)
        {
            var sideChainInfo = State.SideChainInfo[input.Value];
            var proposer = sideChainInfo.Proposer;
            SetContractStateRequired(State.AssociationContract, SmartContractConstants.AssociationContractSystemName);
            var organizationCreationInput = GenerateOrganizationInputForIndexingFeePrice(proposer);
            var sideChainIndexingFeeControllerAddress =
                CalculateSideChainIndexingFeeControllerOrganizationAddress(organizationCreationInput);
            return new GetSideChainIndexingFeeControllerOutput
            {
                AuthorityStuff = new AuthorityStuff
                {
                    OwnerAddress = sideChainIndexingFeeControllerAddress,
                    ContractAddress = State.AssociationContract.Value
                },
                OrganizationCreationInputBytes = organizationCreationInput.ToByteString()
            };
        }
    }
}