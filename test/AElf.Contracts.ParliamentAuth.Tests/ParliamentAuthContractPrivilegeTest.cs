using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.ParliamentAuth
{
    public class ParliamentAuthContractPrivilegeTest : ParliamentAuthContractPrivilegeTestBase
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
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                createProposalInput);
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Not authorized to propose.").ShouldBeTrue();
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
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
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
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                createProposalInput);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task CreateProposal_WithoutPrivilege_Test()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var ecKeyPair = CryptoHelper.GenerateKeyPair();
            var otherTester = Tester.CreateNewContractTester(ecKeyPair);
            var transferInput = TransferInput(otherTester.GetCallOwnerAddress());
            var createProposalInput = CreateProposalInput(transferInput, organizationAddress);

            var result = await otherTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                createProposalInput);
            result.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        private async Task<Address> CreateOrganizationAsync()
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
                }
            };
            var transactionResult =
                await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateOrganization),
                    createOrganizationInput);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            return Address.Parser.ParseFrom(transactionResult.ReturnValue);
        }

        private async Task<Address> GetDefaultOrganizationAddressAsync()
        {
            var result = (await Tester.ExecuteContractWithMiningAsync(
                ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.GetDefaultOrganizationAddress),
                new Empty()));
            return Address.Parser.ParseFrom(result.ReturnValue);
        }
    }
}