using System.Collections.Generic;
using System.Linq;
using Acs3;
using Acs7;
using AElf.Contracts.Association;
using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Sdk.CSharp.State;
using AElf.CSharp.Core.Utils;
using AElf.Types;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChain
{
    public partial class CrossChainContract
    {
        private const string ConsensusExtraDataName = "Consensus";

        /// <summary>
        /// Bind parent chain height together with self height.
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
                EncodingHelper.GetBytesFromUtf8String(TransactionResultStatus.Mined.ToString());
            var hash = Hash.FromRawBytes(txId.ToByteArray().Concat(txResultStatusRawBytes).ToArray());
            return path.ComputeRootWithLeafNode(hash);
        }

        private Hash ComputeRootWithMultiHash(IEnumerable<Hash> nodes)
        {
            return BinaryMerkleTree.FromLeafNodes(nodes).Root;
        }

        /// <summary>
        /// Record merkle path of self chain block, which is from parent chain. 
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
            TransferFrom(new TransferFromInput
            {
                From = lockAddress,
                To = Context.Self,
                Amount = amount,
                Symbol = Context.Variables.NativeSymbol
            });
            State.IndexingBalance[chainId] = amount;
        }

        private void UnlockTokenAndResource(SideChainInfo sideChainInfo)
        {
            // unlock token
            var chainId = sideChainInfo.SideChainId;
            var balance = State.IndexingBalance[chainId];
            if (balance != 0)
                Transfer(new TransferInput
                {
                    To = sideChainInfo.Proposer,
                    Amount = balance,
                    Symbol = Context.Variables.NativeSymbol
                });
            State.IndexingBalance[chainId] = 0;
        }

        public void AssertValidSideChainCreationRequest(SideChainCreationRequest sideChainCreationRequest,
            Address proposer)
        {
            Assert(
                sideChainCreationRequest.LockedTokenAmount > 0 &&
                sideChainCreationRequest.LockedTokenAmount > sideChainCreationRequest.IndexingPrice &&
                sideChainCreationRequest.SideChainTokenInitialIssueList.Count > 0 &&
                sideChainCreationRequest.SideChainTokenInitialIssueList.All(issue => issue.Amount > 0),
                "Invalid chain creation request.");
            SetContractStateRequired(State.TokenContract, SmartContractConstants.TokenContractSystemName);
            var allowance = State.TokenContract.GetAllowance.Call(new GetAllowanceInput
            {
                Owner = proposer,
                Spender = Context.Self,
                Symbol = Context.Variables.NativeSymbol
            }).Allowance;
            Assert(allowance >= sideChainCreationRequest.LockedTokenAmount, "Allowance not enough.");
            AssertValidSideChainTokenInfo(sideChainCreationRequest.SideChainTokenSymbol,
                sideChainCreationRequest.SideChainTokenName, sideChainCreationRequest.SideChainTokenTotalSupply);
        }

        private void AssertValidSideChainTokenInfo(string symbol, string tokenName, long totalSupply)
        {
            Assert(!string.IsNullOrEmpty(symbol) && !string.IsNullOrEmpty(tokenName), "Invalid side chain token name,");
            Assert(totalSupply > 0, "Invalid side chain token supply.");
        }

        private void SetContractStateRequired(ContractReferenceState state, Hash contractSystemName)
        {
            if (state.Value != null)
                return;
            state.Value = Context.GetContractAddressByName(contractSystemName);
        }

        private void Transfer(TransferInput input)
        {
            SetContractStateRequired(State.TokenContract, SmartContractConstants.TokenContractSystemName);
            State.TokenContract.Transfer.Send(input);
        }

        private void TransferFrom(TransferFromInput input)
        {
            SetContractStateRequired(State.TokenContract, SmartContractConstants.TokenContractSystemName);
            State.TokenContract.TransferFrom.Send(input);
        }

        private void CreateSideChainToken(SideChainTokenInfo sideChainTokenInfo, int chainId, Address creator)
        {
            SetContractStateRequired(State.TokenContract, SmartContractConstants.TokenContractSystemName);
            State.TokenContract.Create.Send(new CreateInput
            {
                TokenName = sideChainTokenInfo.TokenName,
                Decimals = sideChainTokenInfo.Decimals,
                IsBurnable = sideChainTokenInfo.IsBurnable,
                Issuer = creator,
                IssueChainId = chainId,
                Symbol = sideChainTokenInfo.Symbol,
                TotalSupply = sideChainTokenInfo.TotalSupply
            });
        }

        private void InitialResourceUsage(int chainId, MapField<string, int> initialResourceAmount)
        {
            if (State.ConfigurationContract.Value == null)
            {
                var configurationContractAddress =
                    Context.GetContractAddressByName(SmartContractConstants.ConfigurationContractSystemName);
                if (configurationContractAddress == null)
                {
                    // If Configuration Contract has not deployed, skip following options.
                    return;
                }

                State.ConfigurationContract.Value = configurationContractAddress;
            }

            State.ConfigurationContract.RentResourceTokens.Send(new RentResourceTokensInput
            {
                ChainId = new SInt32Value {Value = chainId},
                ResourceTokenAmount = new ResourceTokenAmount
                {
                    Value = {initialResourceAmount.ToDictionary(i => i.Key, i => i.Value)}
                }
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

        private MinerListWithRoundNumber GetCurrentMiners()
        {
            SetContractStateRequired(State.ConsensusContract, SmartContractConstants.ConsensusContractSystemName);
            var miners = State.ConsensusContract.GetCurrentMinerListWithRoundNumber.Call(new Empty());
            return miners;
        }

        // only for side chain
        private void UpdateCurrentMiners(ByteString bytes)
        {
            SetContractStateRequired(State.ConsensusContract, SmartContractConstants.ConsensusContractSystemName);
            State.ConsensusContract.UpdateConsensusInformation.Send(new ConsensusInformation {Value = bytes});
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
            {
                // it is parent chain
                return GetParentChainMerkleTreeRoot(parentChainHeight);
            }

            if (State.SideChainInfo[chainId] != null)
            {
                // it is child chain
                return GetSideChainMerkleTreeRoot(parentChainHeight);
            }

            return GetCousinChainMerkleTreeRoot(parentChainHeight);
        }

        private AuthorityStuff GetCrossChainIndexingController()
        {
            return State.CrossChainIndexingController.Value;
        }

        private AuthorityStuff GetSideChainLifetimeController()
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

        private void AssertAddressIsParliamentContract(Address address)
        {
            SetContractStateRequired(State.ParliamentContract, SmartContractConstants.ParliamentContractSystemName);
            Assert(State.ParliamentContract.Value == address, "Unauthorized behavior.");
        }

        private void AssertAddressIsCurrentMiner(Address address)
        {
            SetContractStateRequired(State.ConsensusContract, SmartContractConstants.ConsensusContractSystemName);
            var isCurrentMiner = State.ConsensusContract.IsCurrentMiner.Call(address).Value;
            Assert(isCurrentMiner, "No permission.");
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

        private void AssertIsCrossChainBlockDataToBeReleased(RecordCrossChainDataInput recordCrossChainDataInput)
        {
            var pendingProposalExists = TryGetProposalWithStatus(CrossChainIndexingProposalStatus.ToBeReleased,
                out var pendingCrossChainIndexingProposal);
            Assert(
                pendingProposalExists &&
                pendingCrossChainIndexingProposal.Status == CrossChainIndexingProposalStatus.ToBeReleased &&
                pendingCrossChainIndexingProposal.ProposedCrossChainBlockData.Equals(recordCrossChainDataInput
                    .ProposedCrossChainData) &&
                pendingCrossChainIndexingProposal.Proposer == recordCrossChainDataInput.Proposer,
                "Incorrect cross chain indexing proposal status.");
            State.CrossChainIndexingProposal.Value = new CrossChainIndexingProposal();
        }

        private void AssertIsCrossChainBlockDataAlreadyProposed(CrossChainIndexingProposal crossChainIndexingProposal)
        {
            Assert(
                crossChainIndexingProposal.Status == CrossChainIndexingProposalStatus.Proposed &&
                crossChainIndexingProposal.Proposer != null &&
                crossChainIndexingProposal.ProposedCrossChainBlockData != null &&
                crossChainIndexingProposal.ProposalId == null,
                "Incorrect cross chain indexing proposal status.");
        }

        private int GetChainId(long serialNumber)
        {
            return ChainHelper.GetChainId(serialNumber + Context.ChainId);
        }

        private void ProposeNewSideChain(SideChainCreationRequest request, Address proposer)
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
                        Params = new CreateSideChainInput {SideChainCreationRequest = request, Proposer = proposer}
                            .ToByteString(),
                        OrganizationAddress = sideChainLifeTimeController.OwnerAddress
                    },
                OriginProposer = Context.Sender
            };
            Context.SendInline(sideChainLifeTimeController.ContractAddress,
                nameof(AuthorizationContractContainer.AuthorizationContractReferenceState
                    .CreateProposalBySystemContract), proposalCreationInput);
        }

        private void ProposeCrossChainBlockData(CrossChainBlockData crossChainBlockData, Address proposer)
        {
            var crossChainIndexingController = GetCrossChainIndexingController();
            var proposalCreationInput = new CreateProposalBySystemContractInput
            {
                ProposalInput = new CreateProposalInput
                {
                    Params = new RecordCrossChainDataInput
                    {
                        ProposedCrossChainData = crossChainBlockData,
                        Proposer = proposer
                    }.ToByteString(),
                    ContractMethodName = nameof(RecordCrossChainData),
                    ExpiredTime = Context.CurrentBlockTime.AddSeconds(CrossChainIndexingProposalExpirationTimePeriod),
                    OrganizationAddress = crossChainIndexingController.OwnerAddress,
                    ToAddress = Context.Self
                },
                ProposalIdFeedbackMethod = nameof(FeedbackCrossChainIndexingProposalId),
                OriginProposer = Context.Sender
            };

            Context.SendInline(crossChainIndexingController.ContractAddress,
                nameof(AuthorizationContractContainer.AuthorizationContractReferenceState
                    .CreateProposalBySystemContract), proposalCreationInput);
            var crossChainIndexingProposal = new CrossChainIndexingProposal
            {
                Proposer = proposer,
                ProposedCrossChainBlockData = crossChainBlockData
            };
            SetCrossChainIndexingProposalStatus(crossChainIndexingProposal, CrossChainIndexingProposalStatus.Proposed);

            Context.Fire(new CrossChainIndexingDataProposedEvent
            {
                ProposedCrossChainData = crossChainBlockData
            });
        }

        private ProposalOutput GetCrossChainIndexingProposal(Hash proposalId)
        {
            var crossChainIndexingController = GetCrossChainIndexingController();
            return Context.Call<ProposalOutput>(crossChainIndexingController.ContractAddress,
                nameof(AuthorizationContractContainer.AuthorizationContractReferenceState.GetProposal), proposalId);
        }

        private void HandleIndexingProposal(Hash proposalId, CrossChainIndexingProposal crossChainIndexingProposal)
        {
            var proposal = GetCrossChainIndexingProposal(proposalId);
            Assert(proposal.ToBeReleased, "Not approved cross chain indexing proposal.");
            var crossChainIndexingController = GetCrossChainIndexingController();
            Context.SendInline(crossChainIndexingController.ContractAddress,
                nameof(AuthorizationContractContainer.AuthorizationContractReferenceState.Release),
                proposal.ProposalId); // release if ready
            SetCrossChainIndexingProposalStatus(crossChainIndexingProposal,
                CrossChainIndexingProposalStatus.ToBeReleased);
        }

        private void AssertValidCrossChainIndexingProposer(Address proposer)
        {
            AssertAddressIsCurrentMiner(proposer);
            var bannedHeight = State.BannedMinerHeight[proposer];
            var permitted = bannedHeight == 0 ||
                            bannedHeight + CrossChainIndexingBannedBlockHeightInterval > Context.CurrentHeight;
            Assert(permitted, $"Cross chain indexing is banned for address {proposer}");
            if (bannedHeight != 0)
                State.BannedMinerHeight.Remove(proposer); // lift the ban
        }

        private void AssertValidCrossChainDataBeforeIndexing(CrossChainBlockData crossChainBlockData)
        {
            Assert(
                crossChainBlockData.ParentChainBlockDataList.Count > 0 ||
                crossChainBlockData.SideChainBlockDataList.Count > 0,
                "Empty cross chain data proposed.");
            Assert(ValidateSideChainBlockData(crossChainBlockData.SideChainBlockDataList)
                   && ValidateParentChainBlockData(crossChainBlockData.ParentChainBlockDataList),
                "Invalid cross chain data to be indexed.");
        }

        private bool ValidateSideChainBlockData(IEnumerable<SideChainBlockData> sideChainBlockData)
        {
            var groupResult = sideChainBlockData.GroupBy(data => data.ChainId, data => data);

            foreach (var group in groupResult)
            {
                var chainId = group.Key;
                var info = State.SideChainInfo[chainId];
                if (info == null || info.SideChainStatus != SideChainStatus.Active)
                    return false;
                var currentSideChainHeight = State.CurrentSideChainHeight[chainId];
                var target = currentSideChainHeight != 0
                    ? currentSideChainHeight + 1
                    : Constants.GenesisBlockHeight;
                // indexing fee
                var indexingPrice = info.SideChainCreationRequest.IndexingPrice;
                var lockedToken = State.IndexingBalance[chainId];
                foreach (var blockData in group)
                {
                    var sideChainHeight = blockData.Height;
                    if (target != sideChainHeight)
                        return false;
                    target++;
                }

                if (indexingPrice.Mul(group.Count()) > lockedToken)
                    return false;
            }

            return true;
        }

        private bool ValidateParentChainBlockData(IEnumerable<ParentChainBlockData> parentChainBlockData)
        {
            var parentChainId = State.ParentChainId.Value;
            var currentHeight = State.CurrentParentChainHeight.Value;
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

            return true;
        }

        private bool TryGetProposalWithStatus(CrossChainIndexingProposalStatus status,
            out CrossChainIndexingProposal proposal)
        {
            proposal = State.CrossChainIndexingProposal.Value;
            return proposal != null && proposal.Status == status;
        }

        private void ResetCrossChainIndexingProposal()
        {
            // clear pending proposal
            SetCrossChainIndexingProposalStatus(new CrossChainIndexingProposal(),
                CrossChainIndexingProposalStatus.NonProposed);
        }

        private void SetCrossChainIndexingProposalStatus(CrossChainIndexingProposal crossChainIndexingProposal,
            CrossChainIndexingProposalStatus status)
        {
            crossChainIndexingProposal.Status = status;
            State.CrossChainIndexingProposal.Value = crossChainIndexingProposal;
        }

        private void BanCrossChainIndexingFromAddress(Address address)
        {
            State.BannedMinerHeight[address] = Context.CurrentHeight;
        }

        private void ClearExpiredCrossChainIndexingProposalIfExists()
        {
            var crossChainIndexingProposal = State.CrossChainIndexingProposal.Value;
            if (crossChainIndexingProposal.Status == CrossChainIndexingProposalStatus.NonProposed)
                return;

            var isExpired = CheckProposalExpired(crossChainIndexingProposal.ProposalId);
            Assert(isExpired, "Unable to clear cross chain indexing proposal which is not expired.");
//            BanCrossChainIndexingFromAddress(crossChainIndexingProposal.Proposer); // ban the proposer if expired
            ResetCrossChainIndexingProposal();
        }

        private bool TryClearExpiredSideChainCreationRequestProposal(Hash proposalId, Address proposer)
        {
            var isExpired = CheckProposalExpired(proposalId);
            if (isExpired)
                State.ProposedSideChainCreationRequest.Remove(proposer);
            return isExpired;
        }

        private bool CheckProposalExpired(Hash proposalId)
        {
            var proposalInfo = GetCrossChainIndexingProposal(proposalId);
            return proposalInfo.ExpiredTime <= Context.CurrentBlockTime;
        }

        private void CreateInitialOrganizationForInitialControllerAddress()
        {
            SetContractStateRequired(State.ParliamentContract, SmartContractConstants.ParliamentContractSystemName);
            State.ParliamentContract.CreateOrganizationBySystemContract.Send(new CreateOrganizationBySystemContractInput
            {
                OrganizationCreationInput = new Parliament.CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = DefaultMinimalApprovalThreshold,
                        MinimalVoteThreshold = DefaultMinimalVoteThresholdThreshold,
                        MaximalAbstentionThreshold = DefaultMaximalAbstentionThreshold,
                        MaximalRejectionThreshold = DefaultMaximalRejectionThreshold
                    },
                    ProposerAuthorityRequired = false,
                    ParliamentMemberProposingAllowed = true
                },
                OrganizationAddressFeedbackMethod = nameof(SetInitialControllerAddress)
            });
        }

        private Association.CreateOrganizationInput GenerateOrganizationInputForIndexingFeePrice(
            Address sideChainCreator)
        {
            var proposers = new List<Address> {sideChainCreator, GetSideChainLifetimeController().OwnerAddress};
            var createOrganizationInput = new Association.CreateOrganizationInput
            {
                ProposerWhiteList = new ProposerWhiteList
                {
                    Proposers = {proposers}
                },
                OrganizationMemberList = new OrganizationMemberList
                {
                    OrganizationMembers = {proposers}
                },
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = proposers.Count,
                    MinimalVoteThreshold = proposers.Count,
                    MaximalRejectionThreshold = 0,
                    MaximalAbstentionThreshold = 0
                }
            };
            return createOrganizationInput;
        }

        private Address CalculateSideChainIndexingFeeControllerOrganizationAddress(Address sideChainCreator)
        {
            var createOrganizationInput = GenerateOrganizationInputForIndexingFeePrice(sideChainCreator);
            var address = CalculateSideChainIndexingFeeControllerOrganizationAddress(createOrganizationInput);
            return address;
        }

        private Address CalculateSideChainIndexingFeeControllerOrganizationAddress(
            Association.CreateOrganizationInput input)
        {
            SetContractStateRequired(State.AssociationContract, SmartContractConstants.AssociationContractSystemName);
            var address = State.AssociationContract.CalculateOrganizationAddress.Call(input);
            return address;
        }

        private void CreateOrganizationForIndexingFeePriceAdjustment(Address sideChainCreator)
        {
            // be careful that this organization is useless after SideChainLifetimeController changed
            var createOrganizationInput = GenerateOrganizationInputForIndexingFeePrice(sideChainCreator);
            SetContractStateRequired(State.AssociationContract, SmartContractConstants.AssociationContractSystemName);
            State.AssociationContract.CreateOrganization.Send(createOrganizationInput);
        }

        private bool ValidateAuthorityStuffExists(AuthorityStuff authorityStuff)
        {
            return Context.Call<BoolValue>(authorityStuff.ContractAddress,
                nameof(AuthorizationContractContainer.AuthorizationContractReferenceState.ValidateOrganizationExist),
                authorityStuff.OwnerAddress).Value;
        }

        private bool ValidateParliamentOrganization(Address organizationAddress,
            bool isParliamentMemberProposingRequired)
        {
            SetContractStateRequired(State.ParliamentContract, SmartContractConstants.ParliamentContractSystemName);
            var organization = State.ParliamentContract.GetOrganization.Call(organizationAddress);
            return organization != null &&
                   (!isParliamentMemberProposingRequired || organization.ParliamentMemberProposingAllowed);
        }
    }
}