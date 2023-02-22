using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Parliament;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.MultiToken;

public partial class MultiTokenContractTests
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

        var methodFeeController = await TokenContractStub.GetMethodFeeController.CallAsync(new Empty());
        var defaultOrganization =
            await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(
                new Empty());
        methodFeeController.OwnerAddress.ShouldBe(defaultOrganization);

        const string proposalCreationMethodName = nameof(TokenContractStub.ChangeMethodFeeController);
        var proposalId = await CreateProposalAsync(TokenContractAddress,
            methodFeeController.OwnerAddress, proposalCreationMethodName, new AuthorityInfo
            {
                OwnerAddress = organizationAddress,
                ContractAddress = ParliamentContractAddress
            });
        await ApproveWithMinersAsync(proposalId);
        var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
        releaseResult.TransactionResult.Error.ShouldBeNullOrEmpty();
        releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var newMethodFeeController = await TokenContractStub.GetMethodFeeController.CallAsync(new Empty());
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
        var result = await TokenContractStub.ChangeMethodFeeController.SendWithExceptionAsync(
            new AuthorityInfo
            {
                OwnerAddress = organizationAddress,
                ContractAddress = ParliamentContractAddress
            });

        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        result.TransactionResult.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
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

    [Fact]
    public async Task SendInvalidTransactionsTest()
    {
        const string assertionMessage = "This method can only be executed in plugin tx.";

        var txResult =
            (await TokenContractStub.ChargeTransactionFees.SendWithExceptionAsync(new ChargeTransactionFeesInput()))
            .TransactionResult;
        txResult.Error.ShouldContain(assertionMessage);

        txResult =
            (await TokenContractStub.ChargeResourceToken.SendWithExceptionAsync(new ChargeResourceTokenInput()))
            .TransactionResult;
        txResult.Error.ShouldContain(assertionMessage);

        txResult =
            (await TokenContractStub.CheckResourceToken.SendWithExceptionAsync(new Empty()))
            .TransactionResult;
        txResult.Error.ShouldContain(assertionMessage);
    }

    [Fact]
    public async Task SetMethodFee_Success_Test()
    {
        await CreateNativeTokenAsync();
        var methodName = "Transfer";
        var tokenSymbol = NativeTokenInfo.Symbol;
        var basicFee = 100;
        var methodFeeController = await TokenContractStub.GetMethodFeeController.CallAsync(new Empty());
        var proposalMethodName = nameof(TokenContractStub.SetMethodFee);
        var methodFees = new MethodFees
        {
            MethodName = methodName,
            Fees =
            {
                new MethodFee { Symbol = tokenSymbol, BasicFee = basicFee }
            }
        };
        var proposalId = await CreateProposalAsync(TokenContractAddress,
            methodFeeController.OwnerAddress, proposalMethodName, methodFees);
        await ApproveWithMinersAsync(proposalId);
        var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
        releaseResult.TransactionResult.Error.ShouldBeNullOrEmpty();
        releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var afterUpdateMethodFees = await TokenContractStub.GetMethodFee.CallAsync(new StringValue
        {
            Value = methodName
        });
        var tokenFee = afterUpdateMethodFees.Fees.SingleOrDefault(x => x.Symbol == tokenSymbol);
        tokenFee.BasicFee.ShouldBe(basicFee);
    }

    [Fact]
    public async Task SetMethodFee_Fail_Test()
    {
        await CreateNativeTokenAsync();
        var tokenSymbol = NativeTokenInfo.Symbol;
        var methodName = "Transfer";
        // unauthorized
        {
            var basicFee = 100;
            var methodFees = new MethodFees
            {
                MethodName = methodName,
                Fees =
                {
                    new MethodFee { Symbol = tokenSymbol, BasicFee = basicFee }
                }
            };
            var ret = await TokenContractStub.SetMethodFee.SendWithExceptionAsync(methodFees);
            ret.TransactionResult.Error.ShouldContain("Unauthorized to set method fee");
        }

        // invalid fee
        {
            var basicFee = 0;
            var methodFees = new MethodFees
            {
                MethodName = methodName,
                Fees =
                {
                    new MethodFee { Symbol = tokenSymbol, BasicFee = basicFee }
                }
            };
            var ret = await TokenContractStub.SetMethodFee.SendWithExceptionAsync(methodFees);
            ret.TransactionResult.Error.ShouldContain("Invalid amount");
        }

        //invalid token symbol
        {
            var basicFee = 100;
            var methodFees = new MethodFees
            {
                MethodName = methodName,
                Fees =
                {
                    new MethodFee { Symbol = "NOTEXIST", BasicFee = basicFee }
                }
            };
            var ret = await TokenContractStub.SetMethodFee.SendWithExceptionAsync(methodFees);
            ret.TransactionResult.Error.ShouldContain("Token is not found");
        }

        // token is not profitable
        {
            var tokenNotProfitable = "DLS";
            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = tokenNotProfitable,
                TokenName = "name",
                Issuer = DefaultAddress,
                TotalSupply = 1000_000
            });
            var methodFees = new MethodFees
            {
                MethodName = methodName,
                Fees =
                {
                    new MethodFee { Symbol = tokenNotProfitable, BasicFee = 100 }
                }
            };
            var ret = await TokenContractStub.SetMethodFee.SendWithExceptionAsync(methodFees);
            ret.TransactionResult.Error.ShouldContain($"Token {tokenNotProfitable} cannot set as method fee.");
        }
    }

    [Theory]
    [InlineData("ClaimTransactionFees", "DonateResourceToken", "ChargeTransactionFees", "CheckThreshold",
        "CheckResourceToken", "ChargeResourceToken", "CrossChainReceiveToken")]
    public async Task GetMethodFee_No_Fee_Test(params string[] defaultSetMethodNames)
    {
        var methodFeeController = await TokenContractStub.GetMethodFeeController.CallAsync(new Empty());
        var proposalMethodName = nameof(TokenContractStub.SetMethodFee);
        await CreateNativeTokenAsync();
        var tokenSymbol = NativeTokenInfo.Symbol;
        var basicFee = 100;
        foreach (var methodName in defaultSetMethodNames)
        {
            var methodFees = new MethodFees
            {
                MethodName = methodName,
                Fees =
                {
                    new MethodFee { Symbol = tokenSymbol, BasicFee = basicFee }
                }
            };
            var proposalId = await CreateProposalAsync(TokenContractAddress,
                methodFeeController.OwnerAddress, proposalMethodName, methodFees);
            await ApproveWithMinersAsync(proposalId);
            var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var updatedMethodFee = await TokenContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = methodName
            });
            updatedMethodFee.Fees.Count.ShouldBe(0);
        }
    }

    [Theory]
    [InlineData("Create")]
    public async Task GetMethodFee_Fix_Fee_Test(params string[] defaultSetMethodNames)
    {
        var methodFeeController = await TokenContractStub.GetMethodFeeController.CallAsync(new Empty());
        var proposalMethodName = nameof(TokenContractStub.SetMethodFee);
        await CreateNativeTokenAsync();
        var tokenSymbol = NativeTokenInfo.Symbol;
        var basicFee = 100;
        foreach (var methodName in defaultSetMethodNames)
        {
            var beforeFee = (await TokenContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = methodName
            })).Fees.SingleOrDefault(x => x.Symbol == tokenSymbol);
            var methodFees = new MethodFees
            {
                MethodName = methodName,
                Fees =
                {
                    new MethodFee { Symbol = tokenSymbol, BasicFee = basicFee }
                }
            };
            var proposalId = await CreateProposalAsync(TokenContractAddress,
                methodFeeController.OwnerAddress, proposalMethodName, methodFees);
            await ApproveWithMinersAsync(proposalId);
            var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var updatedFee = (await TokenContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = methodName
            })).Fees.SingleOrDefault(x => x.Symbol == tokenSymbol);
            updatedFee.BasicFee.ShouldBe(basicFee);
        }
    }
}