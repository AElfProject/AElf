using System.Threading.Tasks;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Contracts.Parliament;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.TokenHolder
{
    public partial class TokenHolderTests
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

            var methodFeeController = await TokenHolderContractStub.GetMethodFeeController.CallAsync(new Empty());
            var defaultOrganization = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            methodFeeController.OwnerAddress.ShouldBe(defaultOrganization);

            const string proposalCreationMethodName = nameof(TokenHolderContractStub.ChangeMethodFeeController);
            var proposalId = await CreateProposalAsync(TokenHolderContractAddress,
                methodFeeController.OwnerAddress, proposalCreationMethodName, new AuthorityInfo
                {
                    OwnerAddress = organizationAddress,
                    ContractAddress = ParliamentContractAddress
                });
            await ApproveWithMinersAsync(proposalId);
            var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
            releaseResult.TransactionResult.Error.ShouldBeNullOrEmpty();
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var newMethodFeeController = await TokenHolderContractStub.GetMethodFeeController.CallAsync(new Empty());
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
            var result = await TokenHolderContractStub.ChangeMethodFeeController.SendWithExceptionAsync(new AuthorityInfo
            {
                OwnerAddress = organizationAddress,
                ContractAddress = ParliamentContractAddress
            });

            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }

        [Fact]
        public async Task SetMethodFee_Without_Authority_Test()
        {
            var setMethodFeeRet = await TokenHolderContractStub.SetMethodFee.SendWithExceptionAsync(new MethodFees
            {
                MethodName = "Test",
                Fees =
                {
                    new MethodFee
                    {
                        BasicFee = 100,
                        Symbol = "ELF"
                    }
                }
            });
            setMethodFeeRet.TransactionResult.Error.ShouldContain("Unauthorized to set method fee.");
        }
        
        [Fact]
        public async Task SetMethodFee_With_Invalid_Input_Test()
        {
            var methodFees = new MethodFees
            {
                MethodName = "Test",
                Fees =
                {
                    new MethodFee
                    {
                        BasicFee = 100,
                        Symbol = "NOTEXIST"
                    }
                }
            };
            var setMethodFeeRet = await TokenHolderContractStub.SetMethodFee.SendWithExceptionAsync(methodFees);
            setMethodFeeRet.TransactionResult.Error.ShouldContain("Token is not found");
            methodFees.Fees[0].Symbol = "ELF";
            methodFees.Fees[0].BasicFee = -1;
            setMethodFeeRet = await TokenHolderContractStub.SetMethodFee.SendWithExceptionAsync(methodFees);
            setMethodFeeRet.TransactionResult.Error.ShouldContain("Invalid amount");
        }
        
        [Fact]
        public async Task SetMethodFee_Test()
        {
            var testMethodName = "Test";
            var feeAmount = 100;
            var methodFees = new MethodFees
            {
                MethodName = testMethodName,
                Fees =
                {
                    new MethodFee
                    {
                        BasicFee = feeAmount,
                        Symbol = "ELF"
                    }
                }
            };
            var methodFeeController = await TokenHolderContractStub.GetMethodFeeController.CallAsync(new Empty());
            var proposalId = await CreateProposalAsync(TokenHolderContractAddress,
                methodFeeController.OwnerAddress, nameof(TokenHolderContractStub.SetMethodFee), methodFees);
            await ApproveWithMinersAsync(proposalId);
            await ParliamentContractStub.Release.SendAsync(proposalId);
            var getMethodFee = await TokenHolderContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = testMethodName
            });
            getMethodFee.Fees[0].BasicFee.ShouldBe(feeAmount);
        }
        
    }
}