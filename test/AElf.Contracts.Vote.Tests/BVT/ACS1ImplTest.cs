using System.Threading.Tasks;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Contracts.Parliament;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Vote
{
    public partial class VoteTests
    {
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

            var methodFeeController = await VoteContractStub.GetMethodFeeController.CallAsync(new Empty());
            var defaultOrganization = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            methodFeeController.OwnerAddress.ShouldBe(defaultOrganization);

            const string proposalCreationMethodName = nameof(VoteContractStub.ChangeMethodFeeController);
            var proposalId = await CreateProposalAsync(VoteContractAddress,
                methodFeeController.OwnerAddress, proposalCreationMethodName, new AuthorityInfo
                {
                    OwnerAddress = organizationAddress,
                    ContractAddress = ParliamentContractAddress
                });
            await ApproveWithMinersAsync(proposalId);
            var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
            releaseResult.TransactionResult.Error.ShouldBeNullOrEmpty();
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var newMethodFeeController = await VoteContractStub.GetMethodFeeController.CallAsync(new Empty());
            newMethodFeeController.OwnerAddress.ShouldBe(organizationAddress);
        }

        [Fact]
        public async Task ChangeMethodFeeController_WithoutAuth_Test()
        {
            var methodFeeController = await VoteContractStub.GetMethodFeeController.CallAsync(new Empty());
            const string proposalCreationMethodName = nameof(VoteContractStub.ChangeMethodFeeController);
            var proposalId = await CreateProposalAsync(VoteContractAddress,
                methodFeeController.OwnerAddress, proposalCreationMethodName, new AuthorityInfo
                {
                    OwnerAddress = DefaultSender,
                    ContractAddress = ParliamentContractAddress
                });
            await ApproveWithMinersAsync(proposalId);
            var releaseResult = await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId);
            releaseResult.TransactionResult.Error.ShouldContain("Invalid authority input.");
        }

        [Fact]
        public async Task ChangeMethodFeeController_With_Invalid_Organization_Test()
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
            var result = await VoteContractStub.ChangeMethodFeeController.SendWithExceptionAsync(
                new AuthorityInfo
                {
                    OwnerAddress = organizationAddress,
                    ContractAddress = ParliamentContractAddress
                });

            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }

        [Fact]
        public async Task SetMethodFee_With_Invalid_Input_Test()
        {
            // Invalid amount
            {
                var setMethodFeeRet = await VoteContractStub.SetMethodFee.SendWithExceptionAsync(new MethodFees
                {
                    MethodName = "Test",
                    Fees =
                    {
                        new MethodFee
                        {
                            Symbol = "NOTEXIST",
                            BasicFee = -111
                        }
                    }
                });
                setMethodFeeRet.TransactionResult.Error.ShouldContain("Invalid amount.");
            }

            // token does not exist
            {
                var setMethodFeeRet = await VoteContractStub.SetMethodFee.SendWithExceptionAsync(new MethodFees
                {
                    MethodName = "Test",
                    Fees =
                    {
                        new MethodFee
                        {
                            Symbol = "NOTEXIST",
                            BasicFee = 111
                        }
                    }
                });
                setMethodFeeRet.TransactionResult.Error.ShouldContain("Token is not found.");
            }
        }

        [Fact]
        public async Task SetMethodFee_Without_Authority_Test()
        {
            var tokenSymbol = TestTokenSymbol;
            var methodName = "Test";
            var basicFee = 111;
            var setMethodFeeRet = await VoteContractStub.SetMethodFee.SendWithExceptionAsync(new MethodFees
            {
                MethodName = methodName,
                Fees =
                {
                    new MethodFee
                    {
                        Symbol = tokenSymbol,
                        BasicFee = basicFee
                    }
                }
            });
            setMethodFeeRet.TransactionResult.Error.ShouldContain("Unauthorized to set method fee.");
        }

        [Fact]
        public async Task SetMethodFee_Success_Test()
        {
            var tokenSymbol = TestTokenSymbol;
            var methodName = "Test";
            var basicFee = 111;
            var methodFeeController = await VoteContractStub.GetMethodFeeController.CallAsync(new Empty());
            const string proposalCreationMethodName = nameof(VoteContractStub.SetMethodFee);
            var proposalId = await CreateProposalAsync(VoteContractAddress,
                methodFeeController.OwnerAddress, proposalCreationMethodName, new MethodFees
                {
                    MethodName = methodName,
                    Fees =
                    {
                        new MethodFee
                        {
                            Symbol = tokenSymbol,
                            BasicFee = basicFee
                        }
                    }
                });
            await ApproveWithMinersAsync(proposalId);
            var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
            releaseResult.TransactionResult.Error.ShouldBeNullOrEmpty();
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var getMethodFee = await VoteContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = methodName
            });
            getMethodFee.Fees.Count.ShouldBe(1);
            getMethodFee.Fees[0].Symbol.ShouldBe(tokenSymbol);
            getMethodFee.Fees[0].BasicFee.ShouldBe(basicFee);
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

        [Theory]
        [InlineData(false, "Register", 10_00000000)]
        [InlineData(true, "Vote", 0)]
        [InlineData(true, "AddOption", 0)]
        public async Task GetMethodFee_Test(bool isDefault, string methodName, long fee)
        {
            if (!isDefault)
            {
                var getMethodFee = await VoteContractStub.GetMethodFee.CallAsync(new StringValue
                {
                    Value = methodName
                });
                getMethodFee.Fees.Count.ShouldBe(1);
                getMethodFee.Fees[0].Symbol.ShouldBe(TestTokenSymbol);
                getMethodFee.Fees[0].BasicFee.ShouldBe(fee);
            }
            else
            {
                var getMethodFee = await VoteContractStub.GetMethodFee.CallAsync(new StringValue
                {
                    Value = methodName
                });
                getMethodFee.Fees.Count.ShouldBe(1);
                getMethodFee.Fees[0].Symbol.ShouldBe(TestTokenSymbol);
                getMethodFee.Fees[0].BasicFee.ShouldBe(VoteContractConstant.DefaultMethodFee);
            }
                
        }

        private async Task ApproveWithMinersAsync(Hash proposalId)
        {
            foreach (var bp in InitialCoreDataCenterKeyPairs)
            {
                var tester = GetParliamentContractTester(bp);
                var approveResult = await tester.Approve.SendAsync(proposalId);
                approveResult.TransactionResult.Error.ShouldBeNullOrEmpty();
            }
        }
    }
}