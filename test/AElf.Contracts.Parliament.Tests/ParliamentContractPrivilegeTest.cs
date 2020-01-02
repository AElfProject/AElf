using System.Threading.Tasks;
using Acs3;
using AElf.Cryptography;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Parliament
{
    public class ParliamentContractPrivilegeTest : ParliamentContractPrivilegeTestBase
    {
        [Fact]
        public async Task CreateProposal_WithPrivileged_Test()
        {
            var organizationAddress = await GetDefaultOrganizationAddressAsync();
            var ecKeyPair = CryptoHelper.GenerateKeyPair();
            var otherTester = Tester.CreateNewContractTester(ecKeyPair);
            var transferInput = TransferInput(otherTester.GetCallOwnerAddress());
            var createProposalInput = CreateProposalInput(transferInput, organizationAddress);

            var result = await otherTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateProposal),
                createProposalInput);
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Unauthorized to propose.").ShouldBeTrue();
        }

        [Fact]
        public async Task CreateProposal_Creator_Test()
        {
            var organizationAddress = await GetDefaultOrganizationAddressAsync();
            var ecKeyPair = CryptoHelper.GenerateKeyPair();
            var otherTester = Tester.CreateNewContractTester(ecKeyPair);
            var transferInput = TransferInput(otherTester.GetCallOwnerAddress());
            var createProposalInput = CreateProposalInput(transferInput, organizationAddress);

            var result = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateProposal),
                createProposalInput);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task CreateProposal_Miner_Test()
        {
            var organizationAddress = await GetDefaultOrganizationAddressAsync();
            var miner = Tester.CreateNewContractTester(Tester.InitialMinerList[0]);
            var transferInput = TransferInput(miner.GetCallOwnerAddress());
            var createProposalInput = CreateProposalInput(transferInput, organizationAddress);

            var result = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateProposal),
                createProposalInput);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task CreateProposal_WithoutPrivilege_Test()
        {
            var organizationAddress = await CreateOrganizationAsync(true);
            var ecKeyPair = CryptoHelper.GenerateKeyPair();
            var otherTester = Tester.CreateNewContractTester(ecKeyPair);
            var transferInput = TransferInput(otherTester.GetCallOwnerAddress());
            var createProposalInput = CreateProposalInput(transferInput, organizationAddress);

            var result = await otherTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateProposal),
                createProposalInput);
            result.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Change_OrganizationProposalWhiteList_Test()
        {
            var organizationAddress = await GetDefaultOrganizationAddressAsync();
            var result = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.GetProposerWhiteListContext), new Empty());
            var proposers = GetProposerWhiteListContextOutput.Parser.ParseFrom(result.ReturnValue).Proposers;

            proposers.Count.ShouldBe(1);
            proposers.Contains(Tester.GetCallOwnerAddress()).ShouldBeTrue();
            var ecKeyPair = CryptoHelper.GenerateKeyPair();

            var proposerWhiteList = new ProposerWhiteList
            {
                Proposers = {Tester.GetAddress(ecKeyPair)}
            };
            var proposalInput = CreateParliamentProposalInput(proposerWhiteList, organizationAddress);
            var createResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateProposal),
                proposalInput);
            createResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = Hash.Parser.ParseFrom(createResult.ReturnValue);

            var miner = Tester.CreateNewContractTester(Tester.InitialMinerList[0]);
            (await miner.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentContractContainer.ParliamentContractStub.Approve), proposalId)).Status
                .ShouldBe(TransactionResultStatus.Mined);
            miner = Tester.CreateNewContractTester(Tester.InitialMinerList[1]);
            (await miner.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentContractContainer.ParliamentContractStub.Approve), proposalId)).Status
                .ShouldBe(TransactionResultStatus.Mined);
            miner = Tester.CreateNewContractTester(Tester.InitialMinerList[2]);
            (await miner.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentContractContainer.ParliamentContractStub.Approve), proposalId)).Status
                .ShouldBe(TransactionResultStatus.Mined);

            var releaseResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.Release), proposalId);
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);

            result = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.GetProposerWhiteListContext), new Empty());
            proposers = GetProposerWhiteListContextOutput.Parser.ParseFrom(result.ReturnValue).Proposers;
            proposers.Count.ShouldBe(1);
            proposers.Contains(Tester.GetAddress(ecKeyPair)).ShouldBeTrue();
            proposers.Contains(Tester.GetCallOwnerAddress()).ShouldBeFalse();
        }

        private async Task<Address> CreateOrganizationAsync(bool proposerAuthorityRequired = false)
        {
            var minimalApprovalThreshold = 6667;
            var maximalAbstentionThreshold = 2000;
            var maximalRejectionThreshold = 3000;
            var minimalVoteThreshold = 8000;
            var createOrganizationInput = new CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = minimalApprovalThreshold,
                    MaximalAbstentionThreshold = maximalAbstentionThreshold,
                    MaximalRejectionThreshold = maximalRejectionThreshold,
                    MinimalVoteThreshold = minimalVoteThreshold
                },
                ProposerAuthorityRequired = proposerAuthorityRequired
            };
            var transactionResult =
                await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentContractContainer.ParliamentContractStub.CreateOrganization),
                    createOrganizationInput);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            return Address.Parser.ParseFrom(transactionResult.ReturnValue);
        }

        private async Task<Address> GetDefaultOrganizationAddressAsync()
        {
            var result = (await Tester.ExecuteContractWithMiningAsync(
                ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.GetDefaultOrganizationAddress),
                new Empty()));
            return Address.Parser.ParseFrom(result.ReturnValue);
        }
    }
}