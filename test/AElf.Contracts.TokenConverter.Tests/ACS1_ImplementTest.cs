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

namespace AElf.Contracts.TokenConverter
{
    public partial class TokenConverterContractTests
    {
        [Fact]
        public async Task ChangeMethodFeeController_Test()
        {
            //await InitializeParliamentContractAsync();
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

            var methodFeeController = await DefaultStub.GetMethodFeeController.CallAsync(new Empty());
            var defaultOrganization =
                await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(
                    new Empty());
            methodFeeController.OwnerAddress.ShouldBe(defaultOrganization);

            const string proposalCreationMethodName = nameof(DefaultStub.ChangeMethodFeeController);
            var proposalId = await CreateProposalAsync(TokenConverterContractAddress,
                methodFeeController.OwnerAddress, proposalCreationMethodName, new AuthorityInfo
                {
                    OwnerAddress = organizationAddress,
                    ContractAddress = ParliamentContractAddress
                });
            await ApproveWithMinersAsync(proposalId);
            var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
            releaseResult.TransactionResult.Error.ShouldBeNullOrEmpty();
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var newMethodFeeController = await DefaultStub.GetMethodFeeController.CallAsync(new Empty());
            newMethodFeeController.OwnerAddress.ShouldBe(organizationAddress);
        }

        [Fact]
        public async Task ChangeMethodFeeController_WithoutAuth_Test()
        {
            //await InitializeParliamentContractAsync();
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
            var result = await DefaultStub.ChangeMethodFeeController.SendWithExceptionAsync(
                new AuthorityInfo
                {
                    OwnerAddress = organizationAddress,
                    ContractAddress = ParliamentContractAddress
                });

            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }

        [Fact]
        public async Task GetMethodFeeController_Test()
        {
            var defaultController = await DefaultStub.GetMethodFeeController.CallAsync(new Empty());
            defaultController.ContractAddress.ShouldBe(ParliamentContractAddress);
            var defaultParliament = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            defaultController.OwnerAddress.ShouldBe(defaultParliament);
        }
        
        [Fact]
        public async Task ChangeMethodFeeController_With_Invalid_Organization_Test()
        {
            var releaseResult = await ExecuteProposalForParliamentTransactionWithException(
                TokenConverterContractAddress, nameof(DefaultStub.ChangeMethodFeeController), new AuthorityInfo
                {
                    OwnerAddress = DefaultSender,
                    ContractAddress = ParliamentContractAddress
                });
            releaseResult.Error.ShouldContain("Invalid authority input");
        }

        [Fact]
        public async Task SetMethodFee_With_Invalid_Input_Test()
        {
            // Invalid amount
            {
                var setMethodFeeRet = await DefaultStub.SetMethodFee.SendWithExceptionAsync(new MethodFees
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
                var setMethodFeeRet = await DefaultStub.SetMethodFee.SendWithExceptionAsync(new MethodFees
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
            var tokenSymbol = "KYO";
            var methodName = "Test";
            var basicFee = 111;
            await CreateTokenAsync(tokenSymbol, 1000_000L);
            var setMethodFeeRet = await DefaultStub.SetMethodFee.SendWithExceptionAsync(new MethodFees
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
            var tokenSymbol = "KYO";
            var methodName = "Test";
            var basicFee = 111;
            await CreateTokenAsync(tokenSymbol, 1000_000L);
            await ExecuteProposalForParliamentTransaction(TokenConverterContractAddress,
                nameof(TokenConverterContractImplContainer.TokenConverterContractImplStub.SetMethodFee), new MethodFees
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
            var getMethodFee = await DefaultStub.GetMethodFee.CallAsync(new StringValue
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