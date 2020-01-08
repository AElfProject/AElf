using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs3;
using Acs7;
using AElf.Contracts.Association;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Token;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Contracts.CrossChain.Tests
{
    public class CrossChainContractTestBase : AEDPoSExtensionTestBase
    {
        #region Contract Address

        public Address TokenContractAddress =>
            ContractAddresses[TokenSmartContractAddressNameProvider.Name];

        protected Address ParliamentContractAddress =>
            ContractAddresses[ParliamentSmartContractAddressNameProvider.Name];

        protected Address AssociationContractAddress =>
            ContractAddresses[AssociationSmartContractAddressNameProvider.Name];

        public Address CrossChainContractAddress =>
            ContractAddresses[SmartContractConstants.CrossChainContractSystemName];

        public Address ConsensusContractAddress =>
            ContractAddresses[ConsensusSmartContractAddressNameProvider.Name];

        #endregion

        protected ECKeyPair DefaultKeyPair => SampleECKeyPairs.KeyPairs[0];

        protected ECKeyPair AnotherKeyPair => SampleECKeyPairs.KeyPairs.Last();

        protected static List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
            SampleECKeyPairs.KeyPairs.Take(AEDPoSExtensionConstants.InitialKeyPairCount).ToList();

        protected Address DefaultSender => Address.FromPublicKey(DefaultKeyPair.PublicKey);

        protected Address AnotherSenderAddress => Address.FromPublicKey(AnotherKeyPair.PublicKey);

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub ConsensusStub =>
            GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                ContractAddresses[ConsensusSmartContractAddressNameProvider.Name],
                DefaultKeyPair);

        #region Token

        internal TokenContractContainer.TokenContractStub TokenContractStub =>
            GetTester<TokenContractContainer.TokenContractStub>(
                ContractAddresses[TokenSmartContractAddressNameProvider.Name],
                SampleECKeyPairs.KeyPairs[0]);

        #endregion

        #region Paliament

        internal ParliamentContractContainer.ParliamentContractStub ParliamentContractStub =>
            GetParliamentContractTester(DefaultKeyPair);

        internal AssociationContractContainer.AssociationContractStub AssociationContractStub { get; }

        internal ParliamentContractContainer.ParliamentContractStub GetParliamentContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<ParliamentContractContainer.ParliamentContractStub>(ParliamentContractAddress,
                keyPair);
        }

        internal AssociationContractContainer.AssociationContractStub GetAssociationContractStub(ECKeyPair keyPair)
        {
            return GetTester<AssociationContractContainer.AssociationContractStub>(AssociationContractAddress,
                keyPair);
        }

        #endregion

        internal CrossChainContractContainer.CrossChainContractStub CrossChainContractStub =>
            GetCrossChainContractStub(DefaultKeyPair);

        internal CrossChainContractContainer.CrossChainContractStub GetCrossChainContractStub(
            ECKeyPair keyPair)
        {
            return GetTester<CrossChainContractContainer.CrossChainContractStub>(
                CrossChainContractAddress,
                keyPair);
        }

        public CrossChainContractTestBase()
        {
            ContractAddresses = AsyncHelper.RunSync(() => DeploySystemSmartContracts(new List<Hash>
            {
                TokenSmartContractAddressNameProvider.Name,
                ParliamentSmartContractAddressNameProvider.Name,
                SmartContractConstants.CrossChainContractSystemName,
                ConsensusSmartContractAddressNameProvider.Name,
                AssociationSmartContractAddressNameProvider.Name
            }));

            AsyncHelper.RunSync(InitializeTokenAsync);
            AsyncHelper.RunSync(InitializeParliamentContractAsync);

            AssociationContractStub =
                GetTester<AssociationContractContainer.AssociationContractStub>(AssociationContractAddress,
                    DefaultKeyPair);
        }

        protected async Task InitializeCrossChainContractAsync(long parentChainHeightOfCreation = 0,
            int parentChainId = 0, bool withException = false)
        {
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                CrossChainContractStub.Initialize.GetTransaction(new InitializeInput
                {
                    ParentChainId = parentChainId == 0 ? ChainHelper.ConvertBase58ToChainId("AELF") : parentChainId,
                    CreationHeightOnParentChain = parentChainHeightOfCreation
                })
            }, withException);
        }

        internal async Task<int> InitAndCreateSideChainAsync(long parentChainHeightOfCreation = 0,
            int parentChainId = 0, long lockedTokenAmount = 10, bool withException = false)
        {
            await InitializeCrossChainContractAsync(parentChainHeightOfCreation, parentChainId, withException);
            await ApproveBalanceAsync(lockedTokenAmount);
            var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount);
            await ApproveWithMinersAsync(proposalId);

            var releaseTx =
                await CrossChainContractStub.ReleaseSideChainCreation.SendAsync(new ReleaseSideChainCreationInput
                    {ProposalId = proposalId});
            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(releaseTx.TransactionResult.Logs.First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                    .NonIndexed);
            var chainId = sideChainCreatedEvent.ChainId;

            return chainId;
        }

        private async Task InitializeParliamentContractAsync()
        {
            var initializeResult = await ParliamentContractStub.Initialize.SendAsync(
                new Parliament.InitializeInput
                {
                    PrivilegedProposer = DefaultSender,
                    ProposerAuthorityRequired = false
                });
            CheckResult(initializeResult.TransactionResult);
        }

        private async Task InitializeTokenAsync()
        {
            const string symbol = "ELF";
            const long totalSupply = 100_000_000;
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                TokenContractStub.Create.GetTransaction(new CreateInput
                {
                    Symbol = symbol,
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "elf token",
                    TotalSupply = totalSupply,
                    Issuer = DefaultSender,
                }),
                TokenContractStub.Issue.GetTransaction(new IssueInput
                {
                    Symbol = symbol,
                    Amount = totalSupply - 20 * 100_000L,
                    To = DefaultSender,
                    Memo = "Issue token to default user.",
                })
            });
        }

        protected async Task ApproveBalanceAsync(long amount)
        {
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                TokenContractStub.Approve.GetTransaction(new ApproveInput
                {
                    Spender = CrossChainContractAddress,
                    Symbol = "ELF",
                    Amount = amount
                }),
                TokenContractStub.GetAllowance.GetTransaction(new GetAllowanceInput
                {
                    Symbol = "ELF",
                    Owner = DefaultSender,
                    Spender = CrossChainContractAddress
                })
            });
        }

        internal async Task<GetAllowanceOutput> ApproveAndTransferOrganizationBalanceAsync(Address organizationAddress,
            long amount)
        {
            var approveInput = new ApproveInput
            {
                Spender = CrossChainContractAddress,
                Symbol = "ELF",
                Amount = amount
            };
            var proposal = (await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = nameof(TokenContractStub.Approve),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = approveInput.ToByteString(),
                ToAddress = TokenContractAddress,
                OrganizationAddress = organizationAddress
            })).Output;
            await ApproveWithMinersAsync(proposal);
            await ReleaseProposalAsync(proposal);

            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                Symbol = "ELF",
                Amount = amount,
                To = organizationAddress
            });

            var allowance = (await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Symbol = "ELF",
                Owner = organizationAddress,
                Spender = CrossChainContractAddress
            }));

            return allowance;
        }

        internal async Task<Hash> CreateSideChainProposalAsync(long indexingPrice, long lockedTokenAmount)
        {
            var createProposalInput = CreateSideChainCreationRequest(indexingPrice, lockedTokenAmount,
                new SideChainTokenInitialIssue
                {
                    Address = DefaultSender,
                    Amount = 100
                });
            var requestSideChainCreation =
                await CrossChainContractStub.RequestSideChainCreation.SendAsync(createProposalInput);

            var proposalId = ProposalCreated.Parser.ParseFrom(requestSideChainCreation.TransactionResult.Logs
                .First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;
            return proposalId;
        }

        internal async Task<Hash> CreateParliamentProposalAsync(string method, Address organizationAddress,
            IMessage input, Address toAddress = null)
        {
            var proposal = (await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ToAddress = toAddress ?? CrossChainContractAddress,
                ContractMethodName = method,
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = organizationAddress,
                Params = input.ToByteString()
            })).Output;
            return proposal;
        }

        internal async Task<Hash> CreateAssociationProposalAsync(string method, Address organizationAddress,
            Address toAddress,
            IMessage input, AssociationContractContainer.AssociationContractStub authorizationContractStub = null)
        {
            var proposalId = (await (authorizationContractStub ?? AssociationContractStub).CreateProposal.SendAsync(
                new CreateProposalInput
                {
                    ToAddress = toAddress,
                    ContractMethodName = method,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                    OrganizationAddress = organizationAddress,
                    Params = input.ToByteString()
                })).Output;
            return proposalId;
        }

        protected async Task<TransactionResult> ReleaseProposalAsync(Hash proposalId)
        {
            var transaction = await ParliamentContractStub.Release.SendAsync(proposalId);
            return transaction.TransactionResult;
        }

        protected async Task<TransactionResult> ReleaseProposalWithExceptionAsync(Hash proposalId)
        {
            var transaction = await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId);
            return transaction.TransactionResult;
        }

        internal SideChainCreationRequest CreateSideChainCreationRequest(long indexingPrice, long lockedTokenAmount,
            params SideChainTokenInitialIssue[] sideChainTokenInitialIssueList)
        {
            var res = new SideChainCreationRequest
            {
                IndexingPrice = indexingPrice,
                LockedTokenAmount = lockedTokenAmount,
                SideChainTokenDecimals = 2,
                IsSideChainTokenBurnable = true,
                SideChainTokenTotalSupply = 1_000_000_000,
                SideChainTokenSymbol = "TE",
                SideChainTokenName = "TEST",
                SideChainTokenInitialIssueList = {sideChainTokenInitialIssueList}
            };
            return res;
        }

        protected async Task ApproveWithMinersAsync(Hash proposalId)
        {
            foreach (var bp in InitialCoreDataCenterKeyPairs)
            {
                var tester = GetParliamentContractTester(bp);
                var approveResult = await tester.Approve.SendAsync(proposalId);
                CheckResult(approveResult.TransactionResult);
            }
        }

        internal async Task<bool> DoIndexAsync(CrossChainBlockData crossChainBlockData)
        {
            var txRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
            var proposalId = ProposalCreated.Parser
                .ParseFrom(txRes.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            await ApproveWithMinersAsync(proposalId);

            await CrossChainContractStub.ReleaseCrossChainIndexing.SendAsync(proposalId);
            return true;
        }

        internal async Task<Hash> DisposeSideChainProposalAsync(SInt32Value chainId)
        {
            var disposalInput = chainId;
            var organizationAddress =
                (await CrossChainContractStub.GetSideChainLifetimeController.CallAsync(new Empty())).OwnerAddress;
            var proposal = (await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = nameof(CrossChainContractStub.DisposeSideChain),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = disposalInput.ToByteString(),
                ToAddress = CrossChainContractAddress,
                OrganizationAddress = organizationAddress
            })).Output;
            return proposal;
        }

        private void CheckResult(TransactionResult result)
        {
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new Exception(result.Error);
            }
        }

        internal ParentChainBlockData CreateParentChainBlockData(long height, int sideChainId, Hash txMerkleTreeRoot)
        {
            return new ParentChainBlockData
            {
                ChainId = sideChainId,
                Height = height,
                TransactionStatusMerkleTreeRoot = txMerkleTreeRoot
            };
        }
    }
}