using System.Collections.Generic;
using System.Linq;
using Acs3;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Acs7;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChain
{
    public partial class CrossChainContract : CrossChainContractContainer.CrossChainContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.ParentChainId.Value = input.ParentChainId;
            State.CurrentParentChainHeight.Value = input.CreationHeightOnParentChain - 1;
            State.CrossChainIndexingProposal.Value = new CrossChainIndexingProposal
            {
                Status = CrossChainIndexingProposalStatus.NonProposed
            };

            CreateInitialOrganizationForInitialControllerAddress();
            if (Context.CurrentHeight != Constants.GenesisBlockHeight)
                return new Empty();

            State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
            State.GenesisContract.SetContractProposerRequiredState.Send(
                new BoolValue {Value = input.IsPrivilegePreserved});

            return new Empty();
        }

        public override Empty SetInitialControllerAddress(Address input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.Initialized.Value = true;
            var parliamentContractAddress = State.ParliamentContract.Value;
            Assert(parliamentContractAddress == Context.Sender, "No permission.");
            var initialAuthorityStuff = new AuthorityStuff
            {
                OwnerAddress = input,
                ContractAddress = parliamentContractAddress
            };
            State.CrossChainIndexingController.Value = initialAuthorityStuff;
            State.SideChainLifetimeController.Value = initialAuthorityStuff;
            return new Empty();
        }

        #region Side chain lifetime actions

        public override Empty RequestSideChainCreation(SideChainCreationRequest input)
        {
            Assert(State.ProposedSideChainCreationRequest[Context.Sender] == null,
                "Request side chain creation failed.");
            AssertValidSideChainCreationRequest(input, Context.Sender);
            ProposeNewSideChain(input, Context.Sender);
            State.ProposedSideChainCreationRequest[Context.Sender] = input;
            return new Empty();
        }

        public override Empty ReleaseSideChainCreation(ReleaseSideChainCreationInput input)
        {
            var sideChainCreationRequest = State.ProposedSideChainCreationRequest[Context.Sender];
            Assert(sideChainCreationRequest != null, "Release side chain creation failed.");
            if (!TryClearExpiredSideChainCreationRequestProposal(input.ProposalId, Context.Sender))
                Context.SendInline(State.SideChainLifetimeController.Value.ContractAddress,
                    nameof(AuthorizationContractContainer.AuthorizationContractReferenceState.Release),
                    input.ProposalId);
            return new Empty();
        }

        /// <summary>
        /// Create side chain. It is a proposal result from system address.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override SInt32Value CreateSideChain(CreateSideChainInput input)
        {
            // side chain creation should be triggered by organization address.
            AssertSideChainLifetimeControllerAuthority(Context.Sender);

            var proposedSideChainCreationRequest = State.ProposedSideChainCreationRequest[input.Proposer];
            State.ProposedSideChainCreationRequest.Remove(input.Proposer);
            var sideChainCreationRequest = input.SideChainCreationRequest;
            Assert(
                proposedSideChainCreationRequest != null &&
                proposedSideChainCreationRequest.Equals(sideChainCreationRequest),
                "Side chain creation failed without proposed data.");
            AssertValidSideChainCreationRequest(sideChainCreationRequest, input.Proposer);

            State.SideChainSerialNumber.Value = State.SideChainSerialNumber.Value.Add(1);
            var serialNumber = State.SideChainSerialNumber.Value;
            int chainId = GetChainId(serialNumber);

            // lock token
            ChargeSideChainIndexingFee(input.Proposer, sideChainCreationRequest.LockedTokenAmount, chainId);

            var sideChainTokenInfo = new SideChainTokenInfo
            {
                TokenName = sideChainCreationRequest.SideChainTokenName,
                Symbol = sideChainCreationRequest.SideChainTokenSymbol,
                TotalSupply = sideChainCreationRequest.SideChainTokenTotalSupply,
                Decimals = sideChainCreationRequest.SideChainTokenDecimals,
                IsBurnable = sideChainCreationRequest.IsSideChainTokenBurnable
            };
            CreateSideChainToken(sideChainTokenInfo, chainId, input.Proposer);

            var sideChainInfo = new SideChainInfo
            {
                Proposer = input.Proposer,
                SideChainId = chainId,
                SideChainStatus = SideChainStatus.Active,
                SideChainCreationRequest = sideChainCreationRequest,
                CreationTimestamp = Context.CurrentBlockTime,
                CreationHeightOnParentChain = Context.CurrentHeight
            };
            State.SideChainInfo[chainId] = sideChainInfo;
            State.CurrentSideChainHeight[chainId] = 0;

            var initialConsensusInfo = GetCurrentMiners();
            State.SideChainInitialConsensusInfo[chainId] = new BytesValue {Value = initialConsensusInfo.ToByteString()};
            Context.LogDebug(() => $"Initial miner list for side chain {chainId} :" +
                                   string.Join(",",
                                       initialConsensusInfo.MinerList.Pubkeys));
            Context.LogDebug(() => $"RoundNumber {initialConsensusInfo.RoundNumber}");

            var initialResourceAmount = input.SideChainCreationRequest?.InitialResourceAmount;
            if (initialResourceAmount != null)
            {
                InitialResourceUsage(chainId, initialResourceAmount);
            }

            CreateOrganizationForIndexingFeePriceAdjustment(input.Proposer);
            Context.Fire(new SideChainCreatedEvent
            {
                ChainId = chainId,
                Creator = input.Proposer
            });
            return new SInt32Value() {Value = chainId};
        }

        /// <summary>
        /// Recharge for side chain.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty Recharge(RechargeInput input)
        {
            var chainId = input.ChainId;
            var amount = input.Amount;
            var sideChainInfo = State.SideChainInfo[chainId];
            Assert(sideChainInfo != null && sideChainInfo.SideChainStatus == SideChainStatus.Active,
                "Side chain not found or not able to be recharged.");
            State.IndexingBalance[chainId] = State.IndexingBalance[chainId] + amount;

            TransferFrom(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = Context.Variables.NativeSymbol,
                Amount = amount,
                Memo = "Recharge."
            });
            return new Empty();
        }

        /// <summary>
        /// Dispose side chain. It is a proposal result from system address.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override SInt64Value DisposeSideChain(SInt32Value input)
        {
            AssertSideChainLifetimeControllerAuthority(Context.Sender);

            var chainId = input.Value;
            var info = State.SideChainInfo[chainId];
            Assert(info != null, "Side chain not found.");
            Assert(info.SideChainStatus == SideChainStatus.Active, "Incorrect chain status.");

            UnlockTokenAndResource(info);
            info.SideChainStatus = SideChainStatus.Terminated;
            State.SideChainInfo[chainId] = info;
            Context.Fire(new Disposed
            {
                ChainId = chainId
            });
            return new SInt64Value {Value = chainId};
        }

        public override Empty AdjustIndexingFeePrice(AdjustIndexingFeeInput input)
        {
            var info = State.SideChainInfo[input.SideChainId];
            Assert(info != null, "Side chain not found.");
            var sideChainCreator = info.Proposer;
            var expectedOrganizationAddress =
                CalculateSideChainIndexingFeeControllerOrganizationAddress(sideChainCreator);
            Assert(expectedOrganizationAddress == Context.Sender, "No permission.");
            info.SideChainCreationRequest.IndexingPrice = input.IndexingFee;
            State.SideChainInfo[input.SideChainId] = info;
            return new Empty();
        }

        #endregion Side chain lifetime actions

        #region Cross chain indexing actions

        /// <summary>
        /// Propose cross chain block data to be indexed and create a proposal.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty ProposeCrossChainIndexing(CrossChainBlockData input)
        {
            AssertValidCrossChainIndexingProposer(Context.Sender);
            ClearExpiredCrossChainIndexingProposalIfExists();
            AssertValidCrossChainDataBeforeIndexing(input);
            ProposeCrossChainBlockData(input, Context.Sender);
            return new Empty();
        }

        /// <summary>
        /// Feed back from proposal creation as callback to register proposal Id.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty FeedbackCrossChainIndexingProposalId(Hash input)
        {
            AssertAddressIsParliamentContract(Context.Sender);
            var crossChainIndexingProposal = State.CrossChainIndexingProposal.Value;
            AssertIsCrossChainBlockDataAlreadyProposed(crossChainIndexingProposal);
            crossChainIndexingProposal.ProposalId = input;
            SetCrossChainIndexingProposalStatus(crossChainIndexingProposal, CrossChainIndexingProposalStatus.Pending);
            return new Empty();
        }

        /// <summary>
        /// Release cross chain block data proposed before and trigger the proposal to release.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty ReleaseCrossChainIndexing(Hash input)
        {
            AssertValidCrossChainIndexingProposer(Context.Sender);
            var pendingProposalExists = TryGetProposalWithStatus(CrossChainIndexingProposalStatus.Pending,
                out var pendingCrossChainIndexingProposal);
            Assert(pendingProposalExists && pendingCrossChainIndexingProposal.ProposalId == input,
                "Cross chain indexing pending proposal not found.");
            HandleIndexingProposal(pendingCrossChainIndexingProposal.ProposalId, pendingCrossChainIndexingProposal);
            return new Empty();
        }

        public override Empty RecordCrossChainData(RecordCrossChainDataInput input)
        {
            Context.LogDebug(() => "Start RecordCrossChainData.");
            AssertCrossChainIndexingControllerAuthority(Context.Sender);
            AssertIsCrossChainBlockDataToBeReleased(input);

            var indexedParentChainBlockData =
                IndexParentChainBlockData(input.ProposedCrossChainData.ParentChainBlockDataList);

            if (indexedParentChainBlockData.ParentChainBlockDataList.Count > 0)
            {
                State.LastIndexedParentChainBlockData.Value = indexedParentChainBlockData;
                Context.LogDebug(() =>
                    $"Last indexed parent chain height {indexedParentChainBlockData.ParentChainBlockDataList.Last().Height}");
            }

            var indexedSideChainBlockData = IndexSideChainBlockData(
                input.ProposedCrossChainData.SideChainBlockDataList,
                input.Proposer);

            if (indexedSideChainBlockData.SideChainBlockDataList.Count > 0)
            {
                State.IndexedSideChainBlockData.Set(Context.CurrentHeight, indexedSideChainBlockData);
                Context.LogDebug(() =>
                    $"Last indexed side chain height {indexedSideChainBlockData.SideChainBlockDataList.Last().Height}");
                Context.Fire(new SideChainBlockDataIndexedEvent());
            }

            ResetCrossChainIndexingProposal();

            Context.LogDebug(() => "Finished RecordCrossChainData.");

            return new Empty();
        }

        /// <summary>
        /// Index parent chain block data.
        /// </summary>
        /// <param name="parentChainBlockData"></param>
        private IndexedParentChainBlockData IndexParentChainBlockData(IList<ParentChainBlockData> parentChainBlockData)
        {
            var parentChainId = State.ParentChainId.Value;
            var currentHeight = State.CurrentParentChainHeight.Value;
            var indexedParentChainBlockData = new IndexedParentChainBlockData
            {
                LocalChainHeight = Context.CurrentHeight
            };
            for (var i = 0; i < parentChainBlockData.Count; i++)
            {
                var blockInfo = parentChainBlockData[i];
                AssertParentChainBlock(parentChainId, currentHeight, blockInfo);
                long parentChainHeight = blockInfo.Height;
                State.ParentChainTransactionStatusMerkleTreeRoot[parentChainHeight] =
                    blockInfo.TransactionStatusMerkleTreeRoot;
                foreach (var indexedBlockInfo in blockInfo.IndexedMerklePath)
                {
                    BindParentChainHeight(indexedBlockInfo.Key, parentChainHeight);
                    AddIndexedTxRootMerklePathInParentChain(indexedBlockInfo.Key, indexedBlockInfo.Value);
                }

                // send consensus data shared from main chain  
                if (i == parentChainBlockData.Count - 1 &&
                    blockInfo.ExtraData.TryGetValue(ConsensusExtraDataName, out var bytes))
                {
                    Context.LogDebug(() => "Updating consensus information..");
                    UpdateCurrentMiners(bytes);
                }

                if (blockInfo.CrossChainExtraData != null)
                    State.TransactionMerkleTreeRootRecordedInParentChain[parentChainHeight] =
                        blockInfo.CrossChainExtraData.TransactionStatusMerkleTreeRoot;

                indexedParentChainBlockData.ParentChainBlockDataList.Add(blockInfo);
                currentHeight += 1;
            }

            State.CurrentParentChainHeight.Value = currentHeight;
            return indexedParentChainBlockData;
        }

        /// <summary>
        /// Index side chain block data.
        /// </summary>
        /// <param name="sideChainBlockData">Side chain block data to be indexed.</param>
        /// <param name="proposer">Charge indexing fee for the one who proposed side chain block data.</param>
        /// <returns>Valid side chain block data which are indexed.</returns>
        private IndexedSideChainBlockData IndexSideChainBlockData(IList<SideChainBlockData> sideChainBlockData,
            Address proposer)
        {
            var indexedSideChainBlockData = new IndexedSideChainBlockData();
            foreach (var blockInfo in sideChainBlockData)
            {
                var chainId = blockInfo.ChainId;
                var info = State.SideChainInfo[chainId];
                if (info == null || info.SideChainStatus != SideChainStatus.Active)
                    continue;
                var currentSideChainHeight = State.CurrentSideChainHeight[chainId];

                var target = currentSideChainHeight != 0
                    ? currentSideChainHeight + 1
                    : Constants.GenesisBlockHeight;
                long sideChainHeight = blockInfo.Height;
                if (target != sideChainHeight)
                    continue;

                // indexing fee
                var indexingPrice = info.SideChainCreationRequest.IndexingPrice;
                var lockedToken = State.IndexingBalance[chainId];

                lockedToken -= indexingPrice;
                State.IndexingBalance[chainId] = lockedToken;

                if (lockedToken < indexingPrice)
                {
                    info.SideChainStatus = SideChainStatus.Terminated;
                }

                State.SideChainInfo[chainId] = info;

                if (indexingPrice > 0)
                {
                    Transfer(new TransferInput
                    {
                        To = proposer,
                        Symbol = Context.Variables.NativeSymbol,
                        Amount = indexingPrice,
                        Memo = "Index fee."
                    });
                }

                State.CurrentSideChainHeight[chainId] = sideChainHeight;
                indexedSideChainBlockData.SideChainBlockDataList.Add(blockInfo);
            }

            return indexedSideChainBlockData;
        }

        #endregion Cross chain actions

        public override Empty ChangeCrossChainIndexingController(AuthorityStuff input)
        {
            AssertCrossChainIndexingControllerAuthority(Context.Sender);
            SetContractStateRequired(State.ParliamentContract, SmartContractConstants.ParliamentContractSystemName);
            Assert(
                input.ContractAddress == State.ParliamentContract.Value &&
                ValidateParliamentOrganization(input.OwnerAddress, true), "Invalid authority input.");
            State.CrossChainIndexingController.Value = input;
            return new Empty();
        }

        public override Empty ChangeSideChainLifetimeController(AuthorityStuff input)
        {
            AssertSideChainLifetimeControllerAuthority(Context.Sender);
            Assert(ValidateAuthorityStuffExists(input), "Invalid authority input.");
            State.SideChainLifetimeController.Value = input;
            return new Empty();
        }
    }
}