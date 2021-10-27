using System.Linq;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Standards.ACS7;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElf.CSharp.Core;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChain
{
    public partial class CrossChainContract : CrossChainContractImplContainer.CrossChainContractImplBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.ParentChainId.Value = input.ParentChainId;
            State.CurrentParentChainHeight.Value = input.CreationHeightOnParentChain - 1;
            State.IndexingPendingProposal.Value = new ProposedCrossChainIndexing();

            CreateInitialOrganizationForInitialControllerAddress();
            State.Initialized.Value = true;

            if (Context.CurrentHeight != AElfConstants.GenesisBlockHeight)
                return new Empty();

            State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
            State.GenesisContract.SetContractProposerRequiredState.Send(
                new BoolValue {Value = input.IsPrivilegePreserved});
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
            {
                var serialNumber = State.SideChainSerialNumber.Value.Add(1);
                var chainId = GetChainId(serialNumber);
                CreateSideChainToken(sideChainCreationRequest.SideChainCreationRequest, chainId, sideChainCreationRequest.Proposer);
                Context.SendInline(State.SideChainLifetimeController.Value.ContractAddress,
                    nameof(AuthorizationContractContainer.AuthorizationContractReferenceState.Release),
                    input.ProposalId);
            }
            
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

            var chainInitializationData =
                GetChainInitializationData(sideChainInfo, sideChainCreationRequest);
            State.SideChainInitializationData[sideChainInfo.SideChainId] = chainInitializationData;

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

            TransferFrom(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.ConvertVirtualAddressToContractAddress(ConvertChainIdToHash(chainId)),
                Symbol = Context.Variables.NativeSymbol,
                Amount = input.Amount,
                Memo = "Indexing fee recharging."
            });

            long arrearsAmount = 0;
            if (sideChainInfo.SideChainStatus == SideChainStatus.IndexingFeeDebt)
            {
                // arrears
                foreach (var arrears in sideChainInfo.ArrearsInfo)
                {
                    arrearsAmount += arrears.Value;
                    TransferDepositToken(new TransferInput
                    {
                        To = Address.Parser.ParseFrom(ByteString.FromBase64(arrears.Key)),
                        Symbol = Context.Variables.NativeSymbol,
                        Amount = arrears.Value,
                        Memo = "Indexing fee recharging."
                    }, chainId);
                }
                
                var originBalance = GetSideChainIndexingFeeDeposit(chainId); 
                Assert(input.Amount + originBalance >= arrearsAmount + sideChainInfo.IndexingPrice,
                    "Indexing fee recharging not enough.");
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

            if (TryGetIndexingProposal(chainId, out _))
                ResetChainIndexingProposal(chainId);
            
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
            var expectedOrganizationAddress = info.IndexingFeeController.OwnerAddress;
            Assert(expectedOrganizationAddress == Context.Sender, "No permission.");
            info.IndexingPrice = input.IndexingFee;
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
            Context.LogDebug(() => "Proposing cross chain data..");
            EnsureTransactionOnlyExecutedOnceInOneBlock();
            AssertAddressIsCurrentMiner(Context.Sender);
            ClearCrossChainIndexingProposalIfExpired();
            var crossChainDataDto = ValidateCrossChainDataBeforeIndexing(input);
            ProposeCrossChainBlockData(crossChainDataDto, Context.Sender);
            return new Empty();
        }

        public override Empty ReleaseCrossChainIndexingProposal(ReleaseCrossChainIndexingProposalInput input)
        {
            Context.LogDebug(() => "Releasing cross chain data..");
            EnsureTransactionOnlyExecutedOnceInOneBlock();
            AssertAddressIsCurrentMiner(Context.Sender);
            Assert(input.ChainIdList.Count > 0, "Empty input not allowed.");
            ReleaseIndexingProposal(input.ChainIdList);
            RecordCrossChainData(input.ChainIdList);
            return new Empty();
        }

        public override Empty AcceptCrossChainIndexingProposal(AcceptCrossChainIndexingProposalInput input)
        {
            AssertCrossChainIndexingControllerAuthority(Context.Sender);
            AssertIsCrossChainBlockDataAccepted(input.ChainId);
            ResetChainIndexingProposal(input.ChainId);
            return new Empty();
        }

        #endregion Cross chain actions

        public override Empty ChangeCrossChainIndexingController(AuthorityInfo input)
        {
            AssertCrossChainIndexingControllerAuthority(Context.Sender);
            SetContractStateRequired(State.ParliamentContract, SmartContractConstants.ParliamentContractSystemName);
            Assert(
                input.ContractAddress == State.ParliamentContract.Value &&
                ValidateParliamentOrganization(input.OwnerAddress), "Invalid authority input.");
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