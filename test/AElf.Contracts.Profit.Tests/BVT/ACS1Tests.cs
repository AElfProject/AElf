using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Contracts.Parliament;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContractTests
    {
        [Fact]
        public async Task ProfitContract_SetMethodFee_WithoutPermission_Test()
        {
            var transactionResult = await ProfitContractStub.SetMethodFee.SendWithExceptionAsync(new MethodFees
            {
                MethodName = "OnlyTest"
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.ShouldContain("Unauthorized to set method fee.");
        }

        [Fact]
        public async Task ProfitContract_SetMethodFee_Success_Test()
        {
            var methodName = "OnlyTest";
            var fee = 100;
            var tokenSymbol = "ELF";
            var methodFeeController = await ProfitContractStub.GetMethodFeeController.CallAsync(new Empty());
            var proposalId = await CreateProposalAsync(ProfitContractAddress,
                methodFeeController.OwnerAddress, nameof(ProfitContractStub.SetMethodFee), new MethodFees
                {
                    MethodName = methodName,
                    Fees =
                    {
                        new MethodFee
                        {
                            Symbol = tokenSymbol,
                            BasicFee = fee
                        }
                    }
                });
            await ApproveWithMinersAsync(proposalId);
            await ParliamentContractStub.Release.SendAsync(proposalId);
            var getMethodFee = await ProfitContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = methodName
            });
            getMethodFee.Fees.Count.ShouldBe(1);
            getMethodFee.Fees[0].Symbol.ShouldBe(tokenSymbol);
            getMethodFee.Fees[0].BasicFee.ShouldBe(fee);
        }

        [Fact]
        public async Task ChangeMethodFeeController_Test()
        {
            var createOrganizationResult =
                await ParliamentContractStub.CreateOrganization.SendAsync(
                    new CreateOrganizationInput
                    {
                        ProposalReleaseThreshold = new ProposalReleaseThreshold
                        {
                            MinimalApprovalThreshold = 1000,
                            MinimalVoteThreshold = 1000
                        }
                    });
            var organizationAddress = Address.Parser.ParseFrom(createOrganizationResult.TransactionResult.ReturnValue);

            var methodFeeController = await ProfitContractStub.GetMethodFeeController.CallAsync(new Empty());
            var defaultOrganization = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            methodFeeController.OwnerAddress.ShouldBe(defaultOrganization);

            const string proposalCreationMethodName = nameof(ProfitContractStub.ChangeMethodFeeController);
            var proposalId = await CreateProposalAsync(ProfitContractAddress,
                methodFeeController.OwnerAddress, proposalCreationMethodName, new AuthorityInfo
                {
                    OwnerAddress = organizationAddress,
                    ContractAddress = ParliamentContractAddress
                });
            await ApproveWithMinersAsync(proposalId);
            var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
            releaseResult.TransactionResult.Error.ShouldBeNullOrEmpty();
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var newMethodFeeController = await ProfitContractStub.GetMethodFeeController.CallAsync(new Empty());
            newMethodFeeController.OwnerAddress.ShouldBe(organizationAddress);
        }

        [Fact]
        public async Task ChangeMethodFeeController_WithoutAuth_Test()
        {
            var createOrganizationResult =
                await ParliamentContractStub.CreateOrganization.SendAsync(
                    new CreateOrganizationInput
                    {
                        ProposalReleaseThreshold = new ProposalReleaseThreshold
                        {
                            MinimalApprovalThreshold = 1000,
                            MinimalVoteThreshold = 1000
                        }
                    });
            var organizationAddress = Address.Parser.ParseFrom(createOrganizationResult.TransactionResult.ReturnValue);
            var result = await ProfitContractStub.ChangeMethodFeeController.SendWithExceptionAsync(new AuthorityInfo
            {
                OwnerAddress = organizationAddress,
                ContractAddress = ParliamentContractAddress
            });

            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }

        [Fact]
        public async Task GetMethodFee_Test()
        {
            var methodFee = await ProfitContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(ProfitContractStub.CreateScheme)
            });
            methodFee.Fees[0].BasicFee.ShouldBe(10_00000000);

            var defaultMethodFee = await ProfitContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = "Test"
            });
            defaultMethodFee.Fees[0].BasicFee.ShouldBe(1_00000000);
        }

        private async Task<Hash> CreateProposalAsync(Address contractAddress, Address organizationAddress,
            string methodName, IMessage input)
        {
            var proposal = new CreateProposalInput
            {
                OrganizationAddress = organizationAddress,
                ContractMethodName = methodName,
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
                Params = input.ToByteString(),
                ToAddress = contractAddress
            };

            var createResult = await ParliamentContractStub.CreateProposal.SendAsync(proposal);
            var proposalId = createResult.Output;

            return proposalId;
        }

        private async Task ApproveWithMinersAsync(Hash proposalId)
        {
            foreach (var bp in CreatorKeyPair)
            {
                var tester = GetParliamentContractTester(bp);
                var approveResult = await tester.Approve.SendAsync(proposalId);
                approveResult.TransactionResult.Error.ShouldBeNullOrEmpty();
            }
        }
    }
}