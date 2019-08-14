using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs3;
using Acs7;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Contracts.TestKit;
using AElf.CrossChain;
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

        protected Address ParliamentAuthContractAddress =>
            ContractAddresses[ParliamentAuthSmartContractAddressNameProvider.Name];

        public Address CrossChainContractAddress =>
            ContractAddresses[CrossChainSmartContractAddressNameProvider.Name];

        public Address ConsensusContractAddress =>
            ContractAddresses[ConsensusSmartContractAddressNameProvider.Name];

        #endregion

        protected ECKeyPair DefaultKeyPair => SampleECKeyPairs.KeyPairs[0];

        protected static List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
            SampleECKeyPairs.KeyPairs.Take(AEDPoSExtensionConstants.InitialKeyPairCount).ToList();

        protected Address DefaultSender => Address.FromPublicKey(DefaultKeyPair.PublicKey);

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

        internal ParliamentAuthContractContainer.ParliamentAuthContractStub ParliamentAuthContractStub =>
            GetParliamentAuthContractTester(DefaultKeyPair);

        internal ParliamentAuthContractContainer.ParliamentAuthContractStub GetParliamentAuthContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<ParliamentAuthContractContainer.ParliamentAuthContractStub>(ParliamentAuthContractAddress,
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
                ParliamentAuthSmartContractAddressNameProvider.Name,
                CrossChainSmartContractAddressNameProvider.Name,
                ConsensusSmartContractAddressNameProvider.Name
            }));

            AsyncHelper.RunSync(InitializeTokenAsync);
            AsyncHelper.RunSync(InitializeParliamentContractAsync);
        }

        protected async Task InitializeCrossChainContractAsync(long parentChainHeightOfCreation = 0,
            int parentChainId = 0)
        {
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                CrossChainContractStub.Initialize.GetTransaction(new InitializeInput
                {
                    ParentChainId = parentChainId == 0 ? ChainHelper.ConvertBase58ToChainId("AELF") : parentChainId,
                    CreationHeightOnParentChain = parentChainHeightOfCreation
                })
            });
        }

        protected async Task<int> InitAndCreateSideChainAsync(long parentChainHeightOfCreation = 0,
            int parentChainId = 0, long lockedTokenAmount = 10)
        {
            await InitializeCrossChainContractAsync(parentChainHeightOfCreation, parentChainId);
            await ApproveBalanceAsync(lockedTokenAmount);
            var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount, ByteString.CopyFromUtf8("Test"));
            await ApproveWithMinersAsync(proposalId);

            var transactionResult = await ReleaseProposalAsync(proposalId);
            var chainId = CreationRequested.Parser.ParseFrom(transactionResult.Logs[0].NonIndexed).ChainId;

            return chainId;
        }

        private async Task InitializeParliamentContractAsync()
        {
            var initializeResult = await ParliamentAuthContractStub.Initialize.SendAsync(
                new ParliamentAuth.InitializeInput
                {
                    GenesisOwnerReleaseThreshold = 1,
                    PrivilegedProposer = DefaultSender,
                    ProposerAuthorityRequired = true
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
                TokenContractStub.Approve.GetTransaction(new MultiToken.Messages.ApproveInput
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

        internal async Task<Hash> CreateSideChainProposalAsync(long indexingPrice, long lockedTokenAmount,
            ByteString contractCode, IEnumerable<ResourceTypeBalancePair> resourceTypeBalancePairs = null)
        {
            var createProposalInput = CreateSideChainCreationRequest(indexingPrice, lockedTokenAmount, contractCode);
            var organizationAddress = await ParliamentAuthContractStub.GetGenesisOwnerAddress.CallAsync(new Empty());
            var proposal = await ParliamentAuthContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = "CreateSideChain",
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = createProposalInput.ToByteString(),
                ToAddress = CrossChainContractAddress,
                OrganizationAddress = organizationAddress
            });
            CheckResult(proposal.TransactionResult);
            var proposalId = Hash.Parser.ParseFrom(proposal.TransactionResult.ReturnValue);
            return proposalId;
        }

        protected async Task<TransactionResult> ReleaseProposalAsync(Hash proposalId)
        {
            var transaction = await ParliamentAuthContractStub.Release.SendAsync(proposalId);
            return transaction.TransactionResult;
        }

        internal SideChainCreationRequest CreateSideChainCreationRequest(long indexingPrice, long lockedTokenAmount,
            ByteString contractCode, IEnumerable<ResourceTypeBalancePair> resourceTypeBalancePairs = null)
        {
            var res = new SideChainCreationRequest
            {
                ContractCode = contractCode,
                IndexingPrice = indexingPrice,
                LockedTokenAmount = lockedTokenAmount
            };
//            if (resourceTypeBalancePairs != null)
//                res.ResourceBalances.AddRange(resourceTypeBalancePairs.Select(x =>
//                    ResourceTypeBalancePair.Parser.ParseFrom(x.ToByteString())));
            return res;
        }

        protected async Task ApproveWithMinersAsync(Hash proposalId)
        {
            foreach (var bp in InitialCoreDataCenterKeyPairs)
            {
                var tester = GetParliamentAuthContractTester(bp);
                var approveResult = await tester.Approve.SendAsync(new Acs3.ApproveInput
                {
                    ProposalId = proposalId,
                });
                CheckResult(approveResult.TransactionResult);
            }
        }

        internal async Task<Hash> DisposalSideChainProposalAsync(SInt32Value chainId)
        {
            var disposalInput = chainId;
            var organizationAddress = await ParliamentAuthContractStub.GetGenesisOwnerAddress.CallAsync(new Empty());
            var proposal = (await ParliamentAuthContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = "DisposeSideChain",
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = disposalInput.ToByteString(),
                ToAddress = CrossChainContractAddress,
                OrganizationAddress = organizationAddress
            })).TransactionResult;
            var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
            return proposalId;
        }

        private void CheckResult(TransactionResult result)
        {
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new Exception(result.Error);
            }
        }
    }
}