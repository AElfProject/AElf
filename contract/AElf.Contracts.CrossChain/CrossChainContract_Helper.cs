using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.CSharp.Core.Utils;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS3;
using AElf.Standards.ACS7;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using CreateOrganizationBySystemContractInput = AElf.Contracts.Parliament.CreateOrganizationBySystemContractInput;

namespace AElf.Contracts.CrossChain;

public partial class CrossChainContract
{
    private const string ConsensusExtraDataName = "Consensus";

    /// <summary>
    ///     Bind parent chain height together with self height.
    /// </summary>
    /// <param name="childHeight"></param>
    /// <param name="parentHeight"></param>
    private void BindParentChainHeight(long childHeight, long parentHeight)
    {
        Assert(State.ChildHeightToParentChainHeight[childHeight] == 0,
            $"Already bound at height {childHeight} with parent chain");
        State.ChildHeightToParentChainHeight[childHeight] = parentHeight;
    }

    private Hash ComputeRootWithTransactionStatusMerklePath(Hash txId, MerklePath path)
    {
        var txResultStatusRawBytes =
            EncodingHelper.EncodeUtf8(TransactionResultStatus.Mined.ToString());
        var hash = HashHelper.ComputeFrom(ByteArrayHelper.ConcatArrays(txId.ToByteArray(), txResultStatusRawBytes));
        return path.ComputeRootWithLeafNode(hash);
    }

    private Hash ComputeRootWithMultiHash(IEnumerable<Hash> nodes)
    {
        return BinaryMerkleTree.FromLeafNodes(nodes).Root;
    }

    /// <summary>
    ///     Record merkle path of self chain block, which is from parent chain.
    /// </summary>
    /// <param name="height"></param>
    /// <param name="path"></param>
    private void AddIndexedTxRootMerklePathInParentChain(long height, MerklePath path)
    {
        var existing = State.TxRootMerklePathInParentChain[height];
        Assert(existing == null,
            $"Merkle path already bound at height {height}.");
        State.TxRootMerklePathInParentChain[height] = path;
    }

    private void ChargeSideChainIndexingFee(Address lockAddress, long amount, int chainId)
    {
        if (amount <= 0)
            return;
        TransferFrom(new TransferFromInput
        {
            From = lockAddress,
            To = Context.ConvertVirtualAddressToContractAddress(ConvertChainIdToHash(chainId)),
            Amount = amount,
            Symbol = Context.Variables.NativeSymbol
        });
    }

    private void UnlockTokenAndResource(SideChainInfo sideChainInfo)
    {
        // unlock token
        var chainId = sideChainInfo.SideChainId;
        var balance = GetSideChainIndexingFeeDeposit(chainId);
        if (balance <= 0)
            return;
        TransferDepositToken(new TransferInput
        {
            To = sideChainInfo.Proposer,
            Amount = balance,
            Symbol = Context.Variables.NativeSymbol
        }, chainId);
    }

    private long GetSideChainIndexingFeeDeposit(int chainId)
    {
        SetContractStateRequired(State.TokenContract, SmartContractConstants.TokenContractSystemName);
        var balanceOutput = State.TokenContract.GetBalance.Call(new GetBalanceInput
        {
            Owner = Context.ConvertVirtualAddressToContractAddress(ConvertChainIdToHash(chainId)),
            Symbol = Context.Variables.NativeSymbol
        });

        return balanceOutput.Balance;
    }

    private void AssertValidSideChainCreationRequest(SideChainCreationRequest sideChainCreationRequest,
        Address proposer)
    {
        var proposedRequest = State.ProposedSideChainCreationRequestState[Context.Sender];
        Assert(proposedRequest == null || Context.CurrentBlockTime >= proposedRequest.ExpiredTime,
            "Request side chain creation failed.");

        SetContractStateRequired(State.TokenContract, SmartContractConstants.TokenContractSystemName);
        var allowance = State.TokenContract.GetAllowance.Call(new GetAllowanceInput
        {
            Owner = proposer,
            Spender = Context.Self,
            Symbol = Context.Variables.NativeSymbol
        }).Allowance;

        Assert(
            allowance >= sideChainCreationRequest.LockedTokenAmount,
            "Allowance not enough.");

        Assert(
            sideChainCreationRequest.IndexingPrice >= 0 &&
            sideChainCreationRequest.LockedTokenAmount >= sideChainCreationRequest.IndexingPrice,
            "Invalid chain creation request.");

        if (!sideChainCreationRequest.IsPrivilegePreserved)
            return; // there is no restriction for non-exclusive side chain creation

        AssertValidResourceTokenAmount(sideChainCreationRequest);

        if (!IsPrimaryTokenNeeded(sideChainCreationRequest))
            return;

        // assert primary token to create
        AssertValidSideChainTokenInfo(sideChainCreationRequest.SideChainTokenCreationRequest);
        Assert(sideChainCreationRequest.SideChainTokenInitialIssueList.Count > 0 &&
               sideChainCreationRequest.SideChainTokenInitialIssueList.All(issue => issue.Amount > 0),
            "Invalid side chain token initial issue list.");
    }

    private void AssertValidResourceTokenAmount(SideChainCreationRequest sideChainCreationRequest)
    {
        var resourceTokenMap = sideChainCreationRequest.InitialResourceAmount;
        foreach (var resourceTokenSymbol in Context.Variables.GetStringArray(PayRentalSymbolListName))
            Assert(resourceTokenMap.ContainsKey(resourceTokenSymbol) && resourceTokenMap[resourceTokenSymbol] > 0,
                "Invalid side chain resource token request.");
    }

    private void AssertValidSideChainTokenInfo(SideChainTokenCreationRequest sideChainTokenCreationRequest)
    {
        Assert(
            !string.IsNullOrEmpty(sideChainTokenCreationRequest.SideChainTokenSymbol) &&
            !string.IsNullOrEmpty(sideChainTokenCreationRequest.SideChainTokenName),
            "Invalid side chain token name.");
        Assert(sideChainTokenCreationRequest.SideChainTokenTotalSupply > 0, "Invalid side chain token supply.");
    }

    private void SetContractStateRequired(ContractReferenceState state, string contractSystemName)
    {
        if (state.Value != null)
            return;
        state.Value = Context.GetContractAddressByName(contractSystemName);
    }

    private void TransferDepositToken(TransferInput input, int chainId)
    {
        SetContractStateRequired(State.TokenContract, SmartContractConstants.TokenContractSystemName);
        Context.SendVirtualInline(ConvertChainIdToHash(chainId), State.TokenContract.Value,
            nameof(State.TokenContract.Transfer), input);
    }

    private void TransferFrom(TransferFromInput input)
    {
        SetContractStateRequired(State.TokenContract, SmartContractConstants.TokenContractSystemName);
        State.TokenContract.TransferFrom.Send(input);
    }

    private void CreateSideChainToken(SideChainCreationRequest sideChainCreationRequest, int chainId,
        Address creator)
    {
        if (!IsPrimaryTokenNeeded(sideChainCreationRequest))
            return;

        // new token needed only for exclusive side chain
        SetContractStateRequired(State.TokenContract, SmartContractConstants.TokenContractSystemName);
        State.TokenContract.Create.Send(new CreateInput
        {
            TokenName = sideChainCreationRequest.SideChainTokenCreationRequest.SideChainTokenName,
            Decimals = sideChainCreationRequest.SideChainTokenCreationRequest.SideChainTokenDecimals,
            IsBurnable = true,
            Issuer = creator,
            IssueChainId = chainId,
            Symbol = sideChainCreationRequest.SideChainTokenCreationRequest.SideChainTokenSymbol,
            TotalSupply = sideChainCreationRequest.SideChainTokenCreationRequest.SideChainTokenTotalSupply
        });
    }

    private TokenInfo GetNativeTokenInfo()
    {
        SetContractStateRequired(State.TokenContract, SmartContractConstants.TokenContractSystemName);
        return State.TokenContract.GetNativeTokenInfo.Call(new Empty());
    }

    private TokenInfo GetTokenInfo(string symbol)
    {
        SetContractStateRequired(State.TokenContract, SmartContractConstants.TokenContractSystemName);
        return State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
        {
            Symbol = symbol
        });
    }

    private TokenInfoList GetResourceTokenInfo()
    {
        SetContractStateRequired(State.TokenContract, SmartContractConstants.TokenContractSystemName);
        return State.TokenContract.GetResourceTokenInfo.Call(new Empty());
    }

    private ByteString GetInitialConsensusInformation()
    {
        SetContractStateRequired(State.CrossChainInteractionContract,
            SmartContractConstants.ConsensusContractSystemName);
        var miners = State.CrossChainInteractionContract.GetChainInitializationInformation.Call(new BytesValue());
        return miners.Value;
    }

    // only for side chain
    private void UpdateConsensusInformation(ByteString bytes)
    {
        SetContractStateRequired(State.CrossChainInteractionContract,
            SmartContractConstants.ConsensusContractSystemName);
        Context.SendInline(State.CrossChainInteractionContract.Value,
            nameof(State.CrossChainInteractionContract.UpdateInformationFromCrossChain),
            new BytesValue { Value = bytes });
    }

    private Hash GetParentChainMerkleTreeRoot(long parentChainHeight)
    {
        return State.ParentChainTransactionStatusMerkleTreeRoot[parentChainHeight];
    }

    private Hash GetSideChainMerkleTreeRoot(long parentChainHeight)
    {
        var indexedSideChainData = State.IndexedSideChainBlockData[parentChainHeight];
        return ComputeRootWithMultiHash(
            indexedSideChainData.SideChainBlockDataList.Select(d => d.TransactionStatusMerkleTreeRoot));
    }

    private Hash GetCousinChainMerkleTreeRoot(long parentChainHeight)
    {
        return State.TransactionMerkleTreeRootRecordedInParentChain[parentChainHeight];
    }

    private Hash GetMerkleTreeRoot(int chainId, long parentChainHeight)
    {
        if (chainId == State.ParentChainId.Value)
            // it is parent chain
            return GetParentChainMerkleTreeRoot(parentChainHeight);

        if (State.SideChainInfo[chainId] != null)
            // it is child chain
            return GetSideChainMerkleTreeRoot(parentChainHeight);

        return GetCousinChainMerkleTreeRoot(parentChainHeight);
    }

    private AuthorityInfo GetCrossChainIndexingController()
    {
        return State.CrossChainIndexingController.Value;
    }

    private AuthorityInfo GetSideChainLifetimeController()
    {
        return State.SideChainLifetimeController.Value;
    }

    private void AssertCrossChainIndexingControllerAuthority(Address address)
    {
        var crossChainIndexingController = GetCrossChainIndexingController();
        Assert(crossChainIndexingController.OwnerAddress == address, "Unauthorized behavior.");
    }

    private void AssertSideChainLifetimeControllerAuthority(Address address)
    {
        var sideChainLifetimeController = GetSideChainLifetimeController();
        Assert(sideChainLifetimeController.OwnerAddress == address, "Unauthorized behavior.");
    }

    private void AssertAddressIsCurrentMiner(Address address)
    {
        SetContractStateRequired(State.CrossChainInteractionContract,
            SmartContractConstants.ConsensusContractSystemName);
        var isCurrentMiner = State.CrossChainInteractionContract.CheckCrossChainIndexingPermission.Call(address)
            .Value;
        Assert(isCurrentMiner, "No permission.");
    }

    private void ReleaseIndexingProposal(IEnumerable<int> chainIdList)
    {
        foreach (var chainId in chainIdList)
        {
            var pendingProposalExists = TryGetIndexingProposalWithStatus(chainId,
                CrossChainIndexingProposalStatus.Pending,
                out var pendingCrossChainIndexingProposal);
            Assert(pendingProposalExists, "Chain indexing not proposed.");
            HandleIndexingProposal(pendingCrossChainIndexingProposal.ProposalId);
        }
    }

    private void RecordCrossChainData(IEnumerable<int> chainIdList)
    {
        var indexedSideChainBlockData = new IndexedSideChainBlockData();
        foreach (var chainId in chainIdList)
        {
            var pendingProposalExists = TryGetIndexingProposalWithStatus(chainId,
                CrossChainIndexingProposalStatus.Pending,
                out var pendingCrossChainIndexingProposal);
            Assert(pendingProposalExists, "Chain indexing not proposed.");

            if (chainId == State.ParentChainId.Value)
                IndexParentChainBlockData(pendingCrossChainIndexingProposal.ProposedCrossChainBlockData
                    .ParentChainBlockDataList);
            else
                indexedSideChainBlockData.SideChainBlockDataList.Add(IndexSideChainBlockData(
                    pendingCrossChainIndexingProposal.ProposedCrossChainBlockData.SideChainBlockDataList,
                    pendingCrossChainIndexingProposal.Proposer, chainId));

            SetCrossChainIndexingProposalStatus(pendingCrossChainIndexingProposal,
                CrossChainIndexingProposalStatus.Accepted);
        }

        if (indexedSideChainBlockData.SideChainBlockDataList.Count > 0)
        {
            State.IndexedSideChainBlockData.Set(Context.CurrentHeight, indexedSideChainBlockData);
            Context.Fire(new SideChainBlockDataIndexed());
        }
    }

    private void AssertParentChainBlock(int parentChainId, long currentRecordedHeight,
        ParentChainBlockData parentChainBlockData)
    {
        Assert(parentChainId == parentChainBlockData.ChainId, "Wrong parent chain id.");
        Assert(currentRecordedHeight + 1 == parentChainBlockData.Height,
            $"Parent chain block info at height {currentRecordedHeight + 1} is needed, not {parentChainBlockData.Height}");
        Assert(parentChainBlockData.TransactionStatusMerkleTreeRoot != null,
            "Parent chain transaction status merkle tree root needed.");
    }

    private void AssertIsCrossChainBlockDataAccepted(int chainId)
    {
        var pendingProposalExists =
            TryGetIndexingProposalWithStatus(chainId, CrossChainIndexingProposalStatus.Accepted, out _);
        Assert(pendingProposalExists, "Incorrect cross chain indexing proposal status.");
    }

    private int GetChainId(long serialNumber)
    {
        return ChainHelper.GetChainId(serialNumber + Context.ChainId);
    }

    private SideChainCreationRequestState ProposeNewSideChain(SideChainCreationRequest request, Address proposer)
    {
        var sideChainLifeTimeController = GetSideChainLifetimeController();
        var proposalCreationInput = new CreateProposalBySystemContractInput
        {
            ProposalInput =
                new CreateProposalInput
                {
                    ContractMethodName = nameof(CreateSideChain),
                    ToAddress = Context.Self,
                    ExpiredTime =
                        Context.CurrentBlockTime.AddSeconds(SideChainCreationProposalExpirationTimePeriod),
                    Params = new CreateSideChainInput { SideChainCreationRequest = request, Proposer = proposer }
                        .ToByteString(),
                    OrganizationAddress = sideChainLifeTimeController.OwnerAddress
                },
            OriginProposer = Context.Sender
        };
        Context.SendInline(sideChainLifeTimeController.ContractAddress,
            nameof(AuthorizationContractContainer.AuthorizationContractReferenceState
                .CreateProposalBySystemContract), proposalCreationInput);
        var sideChainCreationRequest = new SideChainCreationRequestState
        {
            SideChainCreationRequest = request,
            ExpiredTime = proposalCreationInput.ProposalInput.ExpiredTime,
            Proposer = proposer
        };
        return sideChainCreationRequest;
    }

    private void ProposeCrossChainBlockData(CrossChainDataDto crossChainDataDto, Address proposer)
    {
        var crossChainIndexingController = GetCrossChainIndexingController();
        foreach (var chainId in crossChainDataDto.GetChainIdList())
        {
            Assert(!TryGetIndexingProposal(chainId, out _), "Chain indexing already proposed.");
            var proposalToken =
                HashHelper.ConcatAndCompute(Context.PreviousBlockHash, ConvertChainIdToHash(chainId));
            var proposalCreationInput = new CreateProposalBySystemContractInput
            {
                ProposalInput = new CreateProposalInput
                {
                    Params = new AcceptCrossChainIndexingProposalInput
                    {
                        ChainId = chainId
                    }.ToByteString(),
                    ContractMethodName = nameof(AcceptCrossChainIndexingProposal),
                    ExpiredTime =
                        Context.CurrentBlockTime.AddSeconds(CrossChainIndexingProposalExpirationTimePeriod),
                    OrganizationAddress = crossChainIndexingController.OwnerAddress,
                    ToAddress = Context.Self,
                    Token = proposalToken
                },
                OriginProposer = Context.Sender
            };

            Context.SendInline(crossChainIndexingController.ContractAddress,
                nameof(AuthorizationContractContainer.AuthorizationContractReferenceState
                    .CreateProposalBySystemContract), proposalCreationInput);

            var proposedCrossChainBlockData = new CrossChainBlockData();
            if (crossChainDataDto.ParentChainToBeIndexedData.TryGetValue(chainId,
                    out var parentChainToBeIndexedData))
                proposedCrossChainBlockData.ParentChainBlockDataList.Add(parentChainToBeIndexedData);
            else if (crossChainDataDto.SideChainToBeIndexedData.TryGetValue(chainId,
                         out var sideChainToBeIndexedData))
                proposedCrossChainBlockData.SideChainBlockDataList.Add(sideChainToBeIndexedData);

            var crossChainIndexingProposal = new ChainIndexingProposal
            {
                ChainId = chainId,
                Proposer = proposer,
                ProposedCrossChainBlockData = proposedCrossChainBlockData
            };
            var proposalId = Context.GenerateId(crossChainIndexingController.ContractAddress, proposalToken);
            crossChainIndexingProposal.ProposalId = proposalId;
            SetCrossChainIndexingProposalStatus(crossChainIndexingProposal,
                CrossChainIndexingProposalStatus.Pending);
            Context.Fire(new CrossChainIndexingDataProposedEvent
            {
                ProposedCrossChainData = proposedCrossChainBlockData,
                ProposalId = proposalId
            });

            Context.LogDebug(() =>
                $"Proposed cross chain data for chain {ChainHelper.ConvertChainIdToBase58(chainId)}");
        }
    }

    private ProposalOutput GetCrossChainProposal(AuthorityInfo authorityInfo, Hash proposalId)
    {
        return Context.Call<ProposalOutput>(authorityInfo.ContractAddress,
            nameof(AuthorizationContractContainer.AuthorizationContractReferenceState.GetProposal), proposalId);
    }

    private void HandleIndexingProposal(Hash proposalId)
    {
        var crossChainIndexingController = GetCrossChainIndexingController();
        var proposal = GetCrossChainProposal(crossChainIndexingController, proposalId);
        Assert(proposal.ToBeReleased, "Not approved cross chain indexing proposal.");
        Context.SendInline(crossChainIndexingController.ContractAddress,
            nameof(AuthorizationContractContainer.AuthorizationContractReferenceState.Release),
            proposal.ProposalId); // release if ready
    }

    private CrossChainDataDto ValidateCrossChainDataBeforeIndexing(CrossChainBlockData crossChainBlockData)
    {
        Assert(
            crossChainBlockData.ParentChainBlockDataList.Count > 0 ||
            crossChainBlockData.SideChainBlockDataList.Count > 0,
            "Empty cross chain data proposed.");
        var validatedParentChainBlockData = new Dictionary<int, List<ParentChainBlockData>>();
        var validationResult = ValidateSideChainBlockData(crossChainBlockData.SideChainBlockDataList,
                                   out var validatedSideChainBlockData) &&
                               ValidateParentChainBlockData(crossChainBlockData.ParentChainBlockDataList,
                                   out validatedParentChainBlockData);
        Assert(validationResult, "Invalid cross chain data to be indexed.");
        var crossChainDataDto = new CrossChainDataDto(validatedSideChainBlockData, validatedParentChainBlockData);

        Assert(crossChainDataDto.GetChainIdList().Count > 0, "Empty cross chain data not allowed.");
        return crossChainDataDto;
    }

    private bool TryGetIndexingProposal(int chainId, out ChainIndexingProposal proposal)
    {
        var proposedIndexingProposal = State.IndexingPendingProposal.Value;
        return proposedIndexingProposal.ChainIndexingProposalCollections.TryGetValue(chainId, out proposal);
    }

    private bool TryGetIndexingProposalWithStatus(int chainId, CrossChainIndexingProposalStatus status,
        out ChainIndexingProposal proposal)
    {
        var proposedIndexingProposal = State.IndexingPendingProposal.Value;
        if (!proposedIndexingProposal.ChainIndexingProposalCollections.TryGetValue(chainId, out proposal))
            return false;
        return proposal.Status == status;
    }

    private void ResetChainIndexingProposal(int chainId)
    {
        // clear pending proposal
        var proposedIndexingProposal = State.IndexingPendingProposal.Value;
        proposedIndexingProposal.ChainIndexingProposalCollections.Remove(chainId);
        State.IndexingPendingProposal.Value = proposedIndexingProposal;
    }

    private void SetCrossChainIndexingProposalStatus(ChainIndexingProposal crossChainIndexingProposal,
        CrossChainIndexingProposalStatus status)
    {
        crossChainIndexingProposal.Status = status;
        var proposedIndexingProposal = State.IndexingPendingProposal.Value;
        proposedIndexingProposal.ChainIndexingProposalCollections[crossChainIndexingProposal.ChainId] =
            crossChainIndexingProposal;
        State.IndexingPendingProposal.Value = proposedIndexingProposal;
    }

    private ChainInitializationData GetChainInitializationData(SideChainInfo sideChainInfo,
        SideChainCreationRequest sideChainCreationRequest)
    {
        SetContractStateRequired(State.TokenContract, SmartContractConstants.TokenContractSystemName);
        var res = new ChainInitializationData
        {
            CreationHeightOnParentChain = sideChainInfo.CreationHeightOnParentChain,
            ChainId = sideChainInfo.SideChainId,
            Creator = sideChainInfo.Proposer,
            CreationTimestamp = sideChainInfo.CreationTimestamp,
            ChainCreatorPrivilegePreserved = sideChainInfo.IsPrivilegePreserved,
            ParentChainTokenContractAddress = State.TokenContract.Value
        };

        var initialConsensusInfo = GetInitialConsensusInformation();
        res.ChainInitializationConsensusInfo = new ChainInitializationConsensusInfo
            { InitialConsensusData = initialConsensusInfo };

        var nativeTokenInformation = GetNativeTokenInfo().ToByteString();
        res.NativeTokenInfoData = nativeTokenInformation;

        var resourceTokenInformation = GetResourceTokenInfo().ToByteString();
        res.ResourceTokenInfo = new ResourceTokenInfo
        {
            ResourceTokenListData = resourceTokenInformation,
            InitialResourceAmount = { sideChainCreationRequest.InitialResourceAmount }
        };

        if (IsPrimaryTokenNeeded(sideChainCreationRequest))
        {
            var sideChainTokenInformation =
                GetTokenInfo(sideChainCreationRequest.SideChainTokenCreationRequest.SideChainTokenSymbol)
                    .ToByteString();
            res.ChainPrimaryTokenInfo = new ChainPrimaryTokenInfo
            {
                ChainPrimaryTokenData = sideChainTokenInformation,
                SideChainTokenInitialIssueList = { sideChainCreationRequest.SideChainTokenInitialIssueList }
            };
        }

        return res;
    }

    private void ClearCrossChainIndexingProposalIfExpired()
    {
        var crossChainIndexingProposal = State.IndexingPendingProposal.Value;
        if (crossChainIndexingProposal == null)
        {
            State.IndexingPendingProposal.Value = new ProposedCrossChainIndexing();
            return;
        }

        foreach (var chainId in crossChainIndexingProposal.ChainIndexingProposalCollections.Keys.ToList())
        {
            var indexingProposal = crossChainIndexingProposal.ChainIndexingProposalCollections[chainId];
            var isExpired = CheckProposalExpired(GetCrossChainIndexingController(), indexingProposal.ProposalId);
            if (isExpired)
                ResetChainIndexingProposal(chainId);
        }
    }

    private bool TryClearExpiredSideChainCreationRequestProposal(Hash proposalId, Address proposer)
    {
        var isExpired = CheckProposalExpired(GetSideChainLifetimeController(), proposalId);
        if (isExpired)
            State.ProposedSideChainCreationRequestState.Remove(proposer);
        return isExpired;
    }

    private bool CheckProposalExpired(AuthorityInfo authorityInfo, Hash proposalId)
    {
        var proposalInfo = GetCrossChainProposal(authorityInfo, proposalId);
        return proposalInfo.ExpiredTime <= Context.CurrentBlockTime;
    }

    private void CreateInitialOrganizationForInitialControllerAddress()
    {
        SetContractStateRequired(State.ParliamentContract, SmartContractConstants.ParliamentContractSystemName);
        var proposalReleaseThreshold = new ProposalReleaseThreshold
        {
            MinimalApprovalThreshold = DefaultMinimalApprovalThreshold,
            MinimalVoteThreshold = DefaultMinimalVoteThresholdThreshold,
            MaximalAbstentionThreshold = DefaultMaximalAbstentionThreshold,
            MaximalRejectionThreshold = DefaultMaximalRejectionThreshold
        };
        State.ParliamentContract.CreateOrganizationBySystemContract.Send(
            new CreateOrganizationBySystemContractInput
            {
                OrganizationCreationInput = new Parliament.CreateOrganizationInput
                {
                    ProposalReleaseThreshold = proposalReleaseThreshold,
                    ProposerAuthorityRequired = false,
                    ParliamentMemberProposingAllowed = true
                },
                OrganizationAddressFeedbackMethod = nameof(SetInitialSideChainLifetimeControllerAddress)
            });

        State.ParliamentContract.CreateOrganizationBySystemContract.Send(
            new CreateOrganizationBySystemContractInput
            {
                OrganizationCreationInput = new Parliament.CreateOrganizationInput
                {
                    ProposalReleaseThreshold = proposalReleaseThreshold,
                    ProposerAuthorityRequired = true,
                    ParliamentMemberProposingAllowed = true
                },
                OrganizationAddressFeedbackMethod = nameof(SetInitialIndexingControllerAddress)
            });
    }

    private CreateOrganizationInput GenerateOrganizationInputForIndexingFeePrice(
        IList<Address> organizationMembers)
    {
        var createOrganizationInput = new CreateOrganizationInput
        {
            ProposerWhiteList = new ProposerWhiteList
            {
                Proposers = { organizationMembers }
            },
            OrganizationMemberList = new OrganizationMemberList
            {
                OrganizationMembers = { organizationMembers }
            },
            ProposalReleaseThreshold = new ProposalReleaseThreshold
            {
                MinimalApprovalThreshold = organizationMembers.ToList().Count,
                MinimalVoteThreshold = organizationMembers.ToList().Count,
                MaximalRejectionThreshold = 0,
                MaximalAbstentionThreshold = 0
            }
        };
        return createOrganizationInput;
    }

    private Address CalculateSideChainIndexingFeeControllerOrganizationAddress(CreateOrganizationInput input)
    {
        SetContractStateRequired(State.AssociationContract, SmartContractConstants.AssociationContractSystemName);
        var address = State.AssociationContract.CalculateOrganizationAddress.Call(input);
        return address;
    }

    private AuthorityInfo CreateDefaultOrganizationForIndexingFeePriceManagement(Address sideChainCreator)
    {
        var createOrganizationInput =
            GenerateOrganizationInputForIndexingFeePrice(new List<Address>
            {
                sideChainCreator,
                GetCrossChainIndexingController().OwnerAddress
            });
        SetContractStateRequired(State.AssociationContract, SmartContractConstants.AssociationContractSystemName);
        State.AssociationContract.CreateOrganization.Send(createOrganizationInput);

        var controllerAddress = CalculateSideChainIndexingFeeControllerOrganizationAddress(createOrganizationInput);
        return new AuthorityInfo
        {
            ContractAddress = State.AssociationContract.Value,
            OwnerAddress = controllerAddress
        };
    }

    private bool ValidateAuthorityInfoExists(AuthorityInfo authorityInfo)
    {
        return Context.Call<BoolValue>(authorityInfo.ContractAddress,
            nameof(AuthorizationContractContainer.AuthorizationContractReferenceState.ValidateOrganizationExist),
            authorityInfo.OwnerAddress).Value;
    }

    private bool ValidateParliamentOrganization(Address organizationAddress)
    {
        SetContractStateRequired(State.ParliamentContract, SmartContractConstants.ParliamentContractSystemName);
        var organization = State.ParliamentContract.GetOrganization.Call(organizationAddress);
        return organization != null && organization.ParliamentMemberProposingAllowed;
    }

    private bool ValidateSideChainBlockData(IEnumerable<SideChainBlockData> sideChainBlockData,
        out Dictionary<int, List<SideChainBlockData>> validatedSideChainBlockData)
    {
        var groupResult = sideChainBlockData.GroupBy(data => data.ChainId, data => data);

        validatedSideChainBlockData = new Dictionary<int, List<SideChainBlockData>>();
        foreach (var group in groupResult)
        {
            var chainId = group.Key;
            validatedSideChainBlockData[chainId] = group.ToList();
            var info = State.SideChainInfo[chainId];
            if (info == null || info.SideChainStatus == SideChainStatus.Terminated)
                return false;
            var currentSideChainHeight = State.CurrentSideChainHeight[chainId];
            var target = currentSideChainHeight != 0
                ? currentSideChainHeight + 1
                : AElfConstants.GenesisBlockHeight;

            foreach (var blockData in group)
            {
                var sideChainHeight = blockData.Height;
                if (target != sideChainHeight)
                    return false;
                target++;
            }
        }

        return true;
    }

    private bool ValidateParentChainBlockData(IList<ParentChainBlockData> parentChainBlockData,
        out Dictionary<int, List<ParentChainBlockData>> validatedParentChainBlockData)
    {
        var parentChainId = State.ParentChainId.Value;
        var currentHeight = State.CurrentParentChainHeight.Value;
        validatedParentChainBlockData = new Dictionary<int, List<ParentChainBlockData>>();
        foreach (var blockData in parentChainBlockData)
        {
            if (parentChainId != blockData.ChainId || currentHeight + 1 != blockData.Height ||
                blockData.TransactionStatusMerkleTreeRoot == null)
                return false;
            if (blockData.IndexedMerklePath.Any(indexedBlockInfo =>
                    State.ChildHeightToParentChainHeight[indexedBlockInfo.Key] != 0 ||
                    State.TxRootMerklePathInParentChain[indexedBlockInfo.Key] != null))
                return false;

            currentHeight += 1;
        }

        if (parentChainBlockData.Count > 0)
            validatedParentChainBlockData[parentChainId] = parentChainBlockData.ToList();

        return true;
    }

    private bool IsPrimaryTokenNeeded(SideChainCreationRequest sideChainCreationRequest)
    {
        // there won't be new token creation if it is secondary side chain
        // or the side chain is not exclusive
        return sideChainCreationRequest.IsPrivilegePreserved && !IsParentChainExist();
    }

    private bool IsParentChainExist()
    {
        return State.ParentChainId.Value != 0;
    }

    /// <summary>
    ///     Index parent chain block data.
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
            var parentChainHeight = blockInfo.Height;
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
                UpdateConsensusInformation(bytes);
            }

            if (blockInfo.CrossChainExtraData != null)
                State.TransactionMerkleTreeRootRecordedInParentChain[parentChainHeight] =
                    blockInfo.CrossChainExtraData.TransactionStatusMerkleTreeRoot;

            indexedParentChainBlockData.ParentChainBlockDataList.Add(blockInfo);
            currentHeight += 1;
        }

        State.CurrentParentChainHeight.Value = currentHeight;
        
        Context.Fire(new ParentChainIndexed
        {
            ChainId = parentChainId,
            IndexedHeight = currentHeight
        });

        return indexedParentChainBlockData;
    }

    /// <summary>
    ///     Index side chain block data.
    /// </summary>
    /// <param name="sideChainBlockDataList">Side chain block data to be indexed.</param>
    /// <param name="proposer">Charge indexing fee for the one who proposed side chain block data.</param>
    /// <param name="chainId">Chain id of side chain to be indexed.</param>
    /// <returns>Valid side chain block data which are indexed.</returns>
    private List<SideChainBlockData> IndexSideChainBlockData(IList<SideChainBlockData> sideChainBlockDataList,
        Address proposer, int chainId)
    {
        var indexedSideChainBlockData = new List<SideChainBlockData>();

        {
            var formattedProposerAddress = proposer.ToByteString().ToBase64();
            long indexingFeeAmount = 0;

            var sideChainInfo = State.SideChainInfo[chainId];
            var currentSideChainHeight = State.CurrentSideChainHeight[chainId];
            long arrearsAmount = 0;
            var lockedToken = sideChainInfo.SideChainStatus == SideChainStatus.IndexingFeeDebt
                ? 0
                : GetSideChainIndexingFeeDeposit(chainId);

            foreach (var sideChainBlockData in sideChainBlockDataList)
            {
                var target = currentSideChainHeight != 0
                    ? currentSideChainHeight + 1
                    : AElfConstants.GenesisBlockHeight;
                var sideChainHeight = sideChainBlockData.Height;
                if (target != sideChainHeight)
                    break;

                // indexing fee
                var indexingPrice = sideChainInfo.IndexingPrice;

                lockedToken -= indexingPrice;

                if (lockedToken < 0)
                {
                    // record arrears
                    arrearsAmount += indexingPrice;
                    sideChainInfo.SideChainStatus = SideChainStatus.IndexingFeeDebt;
                }
                else
                {
                    indexingFeeAmount += indexingPrice;
                }

                currentSideChainHeight++;
                indexedSideChainBlockData.Add(sideChainBlockData);
            }

            if (indexingFeeAmount > 0)
                TransferDepositToken(new TransferInput
                {
                    To = proposer,
                    Symbol = Context.Variables.NativeSymbol,
                    Amount = indexingFeeAmount,
                    Memo = "Index fee."
                }, chainId);

            if (arrearsAmount > 0)
            {
                if (sideChainInfo.ArrearsInfo.TryGetValue(formattedProposerAddress, out var amount))
                    sideChainInfo.ArrearsInfo[formattedProposerAddress] = amount + arrearsAmount;
                else
                    sideChainInfo.ArrearsInfo[formattedProposerAddress] = arrearsAmount;
            }

            State.SideChainInfo[chainId] = sideChainInfo;
            State.CurrentSideChainHeight[chainId] = currentSideChainHeight;
            
            Context.Fire(new SideChainIndexed
            {
                ChainId = chainId,
                IndexedHeight = currentSideChainHeight
            });
        }

        if (indexedSideChainBlockData.Count > 0)
            Context.LogDebug(() =>
                $"Last indexed height {indexedSideChainBlockData.Last().Height} for side chain {chainId}");

        return indexedSideChainBlockData;
    }

    private void EnsureTransactionOnlyExecutedOnceInOneBlock()
    {
        Assert(State.LatestExecutedHeight.Value != Context.CurrentHeight, "Cannot execute this tx.");
        State.LatestExecutedHeight.Value = Context.CurrentHeight;
    }

    private Hash ConvertChainIdToHash(int chainId)
    {
        return HashHelper.ComputeFrom(chainId);
    }
}

internal class CrossChainDataDto
{
    public CrossChainDataDto(Dictionary<int, List<SideChainBlockData>> sideChainToBeIndexedData,
        Dictionary<int, List<ParentChainBlockData>> parentChainToBeIndexedData)
    {
        SideChainToBeIndexedData = sideChainToBeIndexedData;
        ParentChainToBeIndexedData = parentChainToBeIndexedData;
    }

    public Dictionary<int, List<SideChainBlockData>> SideChainToBeIndexedData { get; }

    public Dictionary<int, List<ParentChainBlockData>> ParentChainToBeIndexedData { get; }

    public List<int> GetChainIdList()
    {
        return ParentChainToBeIndexedData.Keys.Concat(SideChainToBeIndexedData.Keys).ToList();
    }
}