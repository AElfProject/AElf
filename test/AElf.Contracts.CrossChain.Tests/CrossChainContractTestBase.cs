using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs3;
using Acs7;
using AElf.Blockchains.BasicBaseChain.ContractNames;
using AElf.Contracts.Association;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;
using SmartContractConstants = AElf.Sdk.CSharp.SmartContractConstants;

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
            ContractAddresses[SmartContractConstants.CrossChainContractSystemHashName];

        public Address ConsensusContractAddress =>
            ContractAddresses[ConsensusSmartContractAddressNameProvider.Name];

        #endregion

        protected ECKeyPair DefaultKeyPair => SampleECKeyPairs.KeyPairs[0];

        protected ECKeyPair AnotherKeyPair => SampleECKeyPairs.KeyPairs.Last();
        protected Address AnotherSender => Address.FromPublicKey(AnotherKeyPair.PublicKey);

        protected static List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
            SampleECKeyPairs.KeyPairs.Take(AEDPoSExtensionConstants.InitialKeyPairCount).ToList();

        protected Address DefaultSender => Address.FromPublicKey(DefaultKeyPair.PublicKey);

        protected Address AnotherSenderAddress => Address.FromPublicKey(AnotherKeyPair.PublicKey);

        protected IBlockTimeProvider BlockTimeProvider =>
            Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub ConsensusStub =>
            GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                ContractAddresses[ConsensusSmartContractAddressNameProvider.Name],
                DefaultKeyPair);

        #region Token

        internal TokenContractContainer.TokenContractStub TokenContractStub =>
            GetTester<TokenContractContainer.TokenContractStub>(
                ContractAddresses[TokenSmartContractAddressNameProvider.Name],
                DefaultKeyPair);

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

        internal TokenContractContainer.TokenContractStub GetTokenContractStub(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(
                TokenContractAddress,
                keyPair);
        }

        protected readonly List<string> ResourceTokenSymbolList;

        public CrossChainContractTestBase()
        {
            ContractAddresses = AsyncHelper.RunSync(() => DeploySystemSmartContracts(new List<Hash>
            {
                TokenSmartContractAddressNameProvider.Name,
                ParliamentSmartContractAddressNameProvider.Name,
                SmartContractConstants.CrossChainContractSystemHashName,
                ConsensusSmartContractAddressNameProvider.Name,
                AssociationSmartContractAddressNameProvider.Name
            }));

            AsyncHelper.RunSync(InitializeTokenAsync);
            AsyncHelper.RunSync(InitializeParliamentContractAsync);

            AssociationContractStub =
                GetTester<AssociationContractContainer.AssociationContractStub>(AssociationContractAddress,
                    DefaultKeyPair);

            ResourceTokenSymbolList = GetRequiredService<IOptionsSnapshot<HostSmartContractBridgeContextOptions>>()
                .Value.ContextVariables["SymbolListToPayRental"].Split(",").ToList();
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
            int parentChainId = 0, long lockedTokenAmount = 10, long indexingFee = 1, ECKeyPair keyPair = null,
            bool withException = false, bool isPrivilegeReserved = false)
        {
            await InitializeCrossChainContractAsync(parentChainHeightOfCreation, parentChainId, withException);
            await ApproveBalanceAsync(lockedTokenAmount, keyPair);
            var proposalId =
                await CreateSideChainProposalAsync(indexingFee, lockedTokenAmount, keyPair, null, isPrivilegeReserved);
            await ApproveWithMinersAsync(proposalId);

            var crossChainContractStub = keyPair == null ? CrossChainContractStub : GetCrossChainContractStub(keyPair);
            var releaseTx =
                await crossChainContractStub.ReleaseSideChainCreation.SendAsync(new ReleaseSideChainCreationInput
                    {ProposalId = proposalId});
            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(releaseTx.TransactionResult.Logs.First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                    .NonIndexed);
            var chainId = sideChainCreatedEvent.ChainId;

            return chainId;
        }
        
        internal async Task<TransactionResult> CreateSideChainByDefaultSenderAsync(bool initCrossChainContract, long parentChainHeightOfCreation = 0,
            int parentChainId = 0, long lockedTokenAmount = 10, long indexingFee = 1, bool isPrivilegeReserved = false)
        {
            if (initCrossChainContract)
                await InitializeCrossChainContractAsync(parentChainHeightOfCreation, parentChainId);
            await ApproveBalanceAsync(lockedTokenAmount);
            var proposalId =
                await CreateSideChainProposalAsync(indexingFee, lockedTokenAmount, null, null, isPrivilegeReserved);
            await ApproveWithMinersAsync(proposalId);
            var releaseTx =
                await CrossChainContractStub.ReleaseSideChainCreation.SendAsync(new ReleaseSideChainCreationInput
                    {ProposalId = proposalId});

            return releaseTx.TransactionResult;
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

        protected async Task ApproveBalanceAsync(long amount, ECKeyPair keyPair = null)
        {
            var tokenContractStub = keyPair == null ? TokenContractStub : GetTokenContractStub(keyPair);
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                tokenContractStub.Approve.GetTransaction(new ApproveInput
                {
                    Spender = CrossChainContractAddress,
                    Symbol = "ELF",
                    Amount = amount
                }),
                tokenContractStub.GetAllowance.GetTransaction(new GetAllowanceInput
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

        internal async Task<Hash> CreateSideChainProposalAsync(long indexingPrice, long lockedTokenAmount,
            ECKeyPair keyPair = null,
            Dictionary<string, int> resourceAmount = null, bool isPrivilegeReserved = false)
        {
            var createProposalInput = CreateSideChainCreationRequest(indexingPrice, lockedTokenAmount,
                resourceAmount ?? GetValidResourceAmount(), new[]
                {
                    new SideChainTokenInitialIssue
                    {
                        Address = DefaultSender,
                        Amount = 100
                    }
                }, isPrivilegeReserved);
            var crossChainContractStub = keyPair == null ? CrossChainContractStub : GetCrossChainContractStub(keyPair);
            var requestSideChainCreation =
                await crossChainContractStub.RequestSideChainCreation.SendAsync(createProposalInput);

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
            Dictionary<string, int> resourceAmount, SideChainTokenInitialIssue[] sideChainTokenInitialIssueList,
            bool isPrivilegePreserved = false)
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
                SideChainTokenInitialIssueList = {sideChainTokenInitialIssueList},
                InitialResourceAmount = {resourceAmount},
                IsPrivilegePreserved = isPrivilegePreserved
            };
            return res;
        }

        internal Dictionary<string, int> GetValidResourceAmount()
        {
            return ResourceTokenSymbolList.ToDictionary(resource => resource, resource => 1);
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

        internal async Task<Hash> DisposeSideChainProposalAsync(Int32Value chainId)
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