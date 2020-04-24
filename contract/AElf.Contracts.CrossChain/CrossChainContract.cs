using System.Linq;
using Acs1;
using Acs3;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Acs7;
using AElf.CSharp.Core;
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
            if (Context.CurrentHeight != AElfConstants.GenesisBlockHeight)
                return new Empty();

            State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
            State.GenesisContract.SetContractProposerRequiredState.Send(
                new BoolValue {Value = input.IsPrivilegePreserved});
            State.Initialized.Value = true;
            return new Empty();
        }

        public override Empty SetInitialSideChainLifetimeControllerAddress(Address input)
        {
            Assert(State.SideChainLifetimeController.Value == null, "Already initialized.");
            var parliamentContractAddress = State.ParliamentContract.Value;
            Assert(parliamentContractAddress == Context.Sender, "No permission.");
            var initialAuthorityInfo = new AuthorityInfo
            {
                OwnerAddress = input,
                ContractAddress = parliamentContractAddress
            };
            State.SideChainLifetimeController.Value = initialAuthorityInfo;
            return new Empty();
        }
        
        public override Empty SetInitialIndexingControllerAddress(Address input)
        {
            Assert(State.CrossChainIndexingController.Value == null, "Already initialized.");
            var parliamentContractAddress = State.ParliamentContract.Value;
            Assert(parliamentContractAddress == Context.Sender, "No permission.");
            var initialAuthorityInfo = new AuthorityInfo
            {
                OwnerAddress = input,
                ContractAddress = parliamentContractAddress
            };
            State.CrossChainIndexingController.Value = initialAuthorityInfo;
            return new Empty();
        }

        #region Side chain lifetime actions

        public override Empty RequestSideChainCreation(SideChainCreationRequest input)
        {
            AssertValidSideChainCreationRequest(input, Context.Sender);
            var sideChainCreationRequestState = ProposeNewSideChain(input, Context.Sender);
            State.ProposedSideChainCreationRequestState[Context.Sender] = sideChainCreationRequestState;
            return new Empty();
        }

        public override Empty ReleaseSideChainCreation(ReleaseSideChainCreationInput input)
        {
            var sideChainCreationRequest = State.ProposedSideChainCreationRequestState[Context.Sender];
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
        public override Int32Value CreateSideChain(CreateSideChainInput input)
        {
            // side chain creation should be triggered by organization address.
            AssertSideChainLifetimeControllerAuthority(Context.Sender);

            var proposedSideChainCreationRequestState = State.ProposedSideChainCreationRequestState[input.Proposer];
            State.ProposedSideChainCreationRequestState.Remove(input.Proposer);
            var sideChainCreationRequest = input.SideChainCreationRequest;
            Assert(
                proposedSideChainCreationRequestState != null &&
                proposedSideChainCreationRequestState.SideChainCreationRequest.Equals(sideChainCreationRequest),
                "Side chain creation failed without proposed data.");
            AssertValidSideChainCreationRequest(sideChainCreationRequest, input.Proposer);

            State.SideChainSerialNumber.Value = State.SideChainSerialNumber.Value.Add(1);
            var serialNumber = State.SideChainSerialNumber.Value;
            var chainId = GetChainId(serialNumber);
            State.AcceptedSideChainCreationRequest[chainId] = sideChainCreationRequest;

            // lock token
            ChargeSideChainIndexingFee(input.Proposer, sideChainCreationRequest.LockedTokenAmount, chainId);
            CreateSideChainToken(sideChainCreationRequest, chainId, input.Proposer);

            var sideChainInfo = new SideChainInfo
            {
                Proposer = input.Proposer,
                SideChainId = chainId,
                SideChainStatus = SideChainStatus.Active,
                IndexingPrice = sideChainCreationRequest.IndexingPrice,
                IsPrivilegePreserved = sideChainCreationRequest.IsPrivilegePreserved,
                CreationTimestamp = Context.CurrentBlockTime,
                CreationHeightOnParentChain = Context.CurrentHeight,
                IndexingFeeController = CreateDefaultOrganizationForIndexingFeePriceManagement(input.Proposer)
            };
            State.SideChainInfo[chainId] = sideChainInfo;
            State.CurrentSideChainHeight[chainId] = 0;

            var initialConsensusInfo = GetCurrentMiners();
            State.SideChainInitialConsensusInfo[chainId] = new BytesValue {Value = initialConsensusInfo.ToByteString()};
            Context.LogDebug(() => $"Initial miner list for side chain {chainId} :" +
                                   string.Join(",",
                                       initialConsensusInfo.MinerList.Pubkeys));

            Context.Fire(new SideChainCreatedEvent
            {
                ChainId = chainId,
                Creator = input.Proposer
            });
            return new Int32Value {Value = chainId};
        }

        /// <summary>
        /// Recharge for side chain.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty Recharge(RechargeInput input)
        {
            var chainId = input.ChainId;
            var sideChainInfo = State.SideChainInfo[chainId];
            Assert(sideChainInfo != null && sideChainInfo.SideChainStatus != SideChainStatus.Terminated,
                "Side chain not found or incorrect side chain status.");
            var oldBalance = State.IndexingBalance[chainId];
            var newBalance = oldBalance + input.Amount;
            Assert(newBalance >= sideChainInfo.IndexingPrice, "Indexing fee recharging not enough.");
            State.IndexingBalance[chainId] = newBalance;

            TransferFrom(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = Context.Variables.NativeSymbol,
                Amount = input.Amount,
                Memo = "Indexing fee recharging."
            });

            if (oldBalance < 0)
            {
                // arrears
                foreach (var arrears in sideChainInfo.ArrearsInfo)
                {
                    Transfer(new TransferInput
                    {
                        To = Address.Parser.ParseFrom(ByteString.FromBase64(arrears.Key)),
                        Symbol = Context.Variables.NativeSymbol,
                        Amount = arrears.Value,
                        Memo = "Indexing fee recharging."
                    });
                }
            }

            sideChainInfo.ArrearsInfo.Clear();
            sideChainInfo.SideChainStatus = SideChainStatus.Active;
            State.SideChainInfo[chainId] = sideChainInfo;
            return new Empty();
        }

        /// <summary>
        /// Dispose side chain. It is a proposal result from system address.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Int32Value DisposeSideChain(Int32Value input)
        {
            AssertSideChainLifetimeControllerAuthority(Context.Sender);

            var chainId = input.Value;
            var info = State.SideChainInfo[chainId];
            Assert(info != null, "Side chain not found.");
            Assert(info.SideChainStatus != SideChainStatus.Terminated, "Incorrect chain status.");

            UnlockTokenAndResource(info);
            info.SideChainStatus = SideChainStatus.Terminated;
            State.SideChainInfo[chainId] = info;
            Context.Fire(new Disposed
            {
                ChainId = chainId
            });
            return new Int32Value {Value = chainId};
        }

        public override Empty AdjustIndexingFeePrice(AdjustIndexingFeeInput input)
        {
            var info = State.SideChainInfo[input.SideChainId];
            Assert(info != null && info.SideChainStatus != SideChainStatus.Terminated,
                "Side chain not found or incorrect side chain status.");
            Assert(input.IndexingFee >= 0, "Invalid side chain fee price.");
            var sideChainCreator = info.Proposer;
            var expectedOrganizationAddress = info.IndexingFeeController.OwnerAddress;
            Assert(expectedOrganizationAddress == Context.Sender, "No permission.");
            info.IndexingPrice = input.IndexingFee;
            var balance = State.IndexingBalance[input.SideChainId];
            if (balance < info.IndexingPrice)
                info.SideChainStatus = SideChainStatus.InsufficientBalance;
            State.SideChainInfo[input.SideChainId] = info;
            return new Empty();
        }

        public override Empty ChangeSideChainIndexingFeeController(ChangeSideChainIndexingFeeControllerInput input)
        {
            var sideChainInfo = State.SideChainInfo[input.ChainId];
            var authorityInfo = sideChainInfo.IndexingFeeController;
            Assert(authorityInfo.OwnerAddress == Context.Sender, "No permission.");
            Assert(ValidateAuthorityInfoExists(input.AuthorityInfo), "Invalid authority input.");
            sideChainInfo.IndexingFeeController = input.AuthorityInfo;
            State.SideChainInfo[input.ChainId] = sideChainInfo;
            Context.Fire(new SideChainIndexingFeeControllerChanged
            {
                ChainId = input.ChainId,
                AuthorityInfo = input.AuthorityInfo
            });
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
            EnsureTransactionOnlyExecutedOnceInOneBlock();
            ClearCrossChainIndexingProposalIfExpired();
            AssertValidCrossChainDataBeforeIndexing(input);
            ProposeCrossChainBlockData(input, Context.Sender);
            return new Empty();
        }

        /// <summary>
        /// Release cross chain block data proposed before and trigger the proposal to release.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty ReleaseCrossChainIndexing(Hash input)
        {
            EnsureTransactionOnlyExecutedOnceInOneBlock();
            AssertAddressIsCurrentMiner(Context.Sender);
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
                Context.Fire(new SideChainBlockDataIndexed());
            }

            ResetCrossChainIndexingProposal();

            Context.LogDebug(() => "Finished RecordCrossChainData.");

            return new Empty();
        }

        #endregion Cross chain actions

        public override Empty ChangeCrossChainIndexingController(AuthorityInfo input)
        {
            AssertCrossChainIndexingControllerAuthority(Context.Sender);
            SetContractStateRequired(State.ParliamentContract, SmartContractConstants.ParliamentContractSystemName);
            Assert(
                input.ContractAddress == State.ParliamentContract.Value &&
                ValidateParliamentOrganization(input.OwnerAddress, true), "Invalid authority input.");
            State.CrossChainIndexingController.Value = input;
            Context.Fire(new CrossChainIndexingControllerChanged
            {
                AuthorityInfo = input
            });
            return new Empty();
        }

        public override Empty ChangeSideChainLifetimeController(AuthorityInfo input)
        {
            AssertSideChainLifetimeControllerAuthority(Context.Sender);
            Assert(ValidateAuthorityInfoExists(input), "Invalid authority input.");
            State.SideChainLifetimeController.Value = input;
            Context.Fire(new SideChainLifetimeControllerChanged()
            {
                AuthorityInfo = input
            });
            return new Empty();
        }
    }
}