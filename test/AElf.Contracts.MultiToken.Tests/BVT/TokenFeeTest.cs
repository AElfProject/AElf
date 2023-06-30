using System.Linq;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.MultiToken;

public partial class MultiTokenContractTests
{
    [Fact(DisplayName = "[MultiToken] advance token not exist in resource token")]
    public async Task AdvancedResourceToken_Test()
    {
       // await CreateNativeTokenAsync();
        long advanceAmount = 1000;
        {
            var tokenNotResrouce = "NORESOURCE";
            await CreateAndIssueCustomizeTokenAsync(DefaultAddress, tokenNotResrouce, 10000, 10000);
            var advanceRet = await TokenContractStub.AdvanceResourceToken.SendWithExceptionAsync(
                new AdvanceResourceTokenInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    Amount = advanceAmount,
                    ResourceTokenSymbol = tokenNotResrouce
                });
            advanceRet.TransactionResult.Error.ShouldContain("invalid resource token symbol");
        }

        {
            var trafficToken = "TRAFFIC";
            await CreateAndIssueCustomizeTokenAsync(DefaultAddress, trafficToken, 10000, 10000);
            var advanceRet = await TokenContractStub.AdvanceResourceToken.SendAsync(
                new AdvanceResourceTokenInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    Amount = advanceAmount,
                    ResourceTokenSymbol = trafficToken
                });
            advanceRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = trafficToken,
                Owner = BasicFunctionContractAddress
            });
            balance.Balance.ShouldBe(advanceAmount);
        }
    }

    [Fact(DisplayName = "[MultiToken] take more token than that of the contract address's balance")]
    public async Task TakeResourceTokenBack_Test()
    {
        var trafficToken = "TRAFFIC";
        var advanceAmount = 1000;
        await CreateAndIssueCustomizeTokenAsync(DefaultAddress, trafficToken, 10000, 10000);
        var advanceRet = await TokenContractStub.AdvanceResourceToken.SendAsync(
            new AdvanceResourceTokenInput
            {
                ContractAddress = BasicFunctionContractAddress,
                Amount = advanceAmount,
                ResourceTokenSymbol = trafficToken
            });
        advanceRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var takeMoreToken = await TokenContractStub.TakeResourceTokenBack.SendWithExceptionAsync(
            new TakeResourceTokenBackInput
            {
                Amount = 99999,
                ContractAddress = BasicFunctionContractAddress,
                ResourceTokenSymbol = trafficToken
            });
        takeMoreToken.TransactionResult.Error.ShouldContain("Can't take back that more");
        await TokenContractStub.TakeResourceTokenBack.SendAsync(
            new TakeResourceTokenBackInput
            {
                Amount = advanceAmount,
                ContractAddress = BasicFunctionContractAddress,
                ResourceTokenSymbol = trafficToken
            });

        var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = trafficToken,
            Owner = BasicFunctionContractAddress
        });
        balance.Balance.ShouldBe(0);
    }

    [Fact(DisplayName = "[MultiToken] illegal controller try to update the coefficientForContract")]
    public async Task UpdateCoefficientForContract_Without_Authorization_Test()
    {
        await CreatePrimaryTokenAsync();
        var updateInfo = new UpdateCoefficientsInput
        {
            Coefficients = new CalculateFeeCoefficients
            {
                FeeTokenType = (int)FeeTypeEnum.Read
            }
        };
        var updateRet =
            await TokenContractStub.UpdateCoefficientsForContract.SendWithExceptionAsync(updateInfo);
        updateRet.TransactionResult.Error.ShouldContain(
            "controller does not initialize, call InitializeAuthorizedController first");
        var initializeControllerRet = await TokenContractStub.InitializeAuthorizedController.SendAsync(new Empty());
        initializeControllerRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        updateRet = await TokenContractStub.UpdateCoefficientsForContract.SendWithExceptionAsync(updateInfo);
        updateRet.TransactionResult.Error.ShouldContain("no permission");
    }

    [Fact(DisplayName = "[MultiToken] Invalid fee type for Update controller")]
    public async Task UpdateCoefficientForContract_With_Invalid_FeeType_Test()
    {
        await CreatePrimaryTokenAsync();
        var updateInfo = new UpdateCoefficientsInput
        {
            Coefficients = new CalculateFeeCoefficients
            {
                FeeTokenType = (int)FeeTypeEnum.Tx
            }
        };
        var initializeControllerRet = await TokenContractStub.InitializeAuthorizedController.SendAsync(new Empty());
        initializeControllerRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var updateRet = await TokenContractStub.UpdateCoefficientsForContract.SendWithExceptionAsync(updateInfo);
        updateRet.TransactionResult.Error.ShouldContain("Invalid fee type");
    }

    [Fact(DisplayName = "[MultiToken] illegal controller try to update the coefficientForSender")]
    public async Task UpdateCoefficientForSender_Without_Authorization_Test()
    {
        await CreatePrimaryTokenAsync();
        var initializeControllerRet = await TokenContractStub.InitializeAuthorizedController.SendAsync(new Empty());
        initializeControllerRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var updateRet =
            await TokenContractStub.UpdateCoefficientsForSender.SendWithExceptionAsync(
                new UpdateCoefficientsInput());
        updateRet.TransactionResult.Error.ShouldContain("no permission");
    }

    [Fact(DisplayName = "[MultiToken] illegal controller try to set the available token list")]
    public async Task SetSymbolsToPayTxSizeFee_Without_Authorization_Test()
    {
        var setSymbolRet =
            await TokenContractStub.SetSymbolsToPayTxSizeFee.SendWithExceptionAsync(new SymbolListToPayTxSizeFee());
        setSymbolRet.TransactionResult.Error.ShouldContain("no permission");
    }

    [Fact(DisplayName = "[MultiToken] Reference Token Fee Controller")]
    public async Task InitializeAuthorizedController_Test()
    {
        await CreatePrimaryTokenAsync();
        var tryToGetControllerInfoRet =
            await TokenContractStub.GetDeveloperFeeController.SendWithExceptionAsync(new Empty());
        tryToGetControllerInfoRet.TransactionResult.Error.ShouldContain(
            "controller does not initialize, call InitializeAuthorizedController first");

        var initializeControllerRet = await TokenContractStub.InitializeAuthorizedController.SendAsync(new Empty());
        initializeControllerRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var afterInitializedDeveloperFeeController =
            await TokenContractStub.GetDeveloperFeeController.CallAsync(new Empty());
        afterInitializedDeveloperFeeController.DeveloperController.ShouldNotBeNull();
        afterInitializedDeveloperFeeController.ParliamentController.ShouldNotBeNull();
        afterInitializedDeveloperFeeController.RootController.ShouldNotBeNull();
    }
    
    [Fact(DisplayName = "[MultiToken] illegal controller try to set free allowances")]
    public async Task ConfigTransactionFeeFreeAllowances_Without_Authorization_Test()
    {
        var configTransactionFeeFreeAllowancesRet =
            await TokenContractStub.ConfigTransactionFeeFreeAllowances.SendWithExceptionAsync(new ConfigTransactionFeeFreeAllowancesInput());
        configTransactionFeeFreeAllowancesRet.TransactionResult.Error.ShouldContain("Unauthorized behavior.");
    }

    [Fact]
    public async Task DonateResourceToken_Without_Authorized_Test()
    {
        var tokenStub =
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, Accounts.Last().KeyPair);
        var donate =
            await tokenStub.DonateResourceToken.SendWithExceptionAsync(new TotalResourceTokensMaps());
        donate.TransactionResult.Error.ShouldContain("No permission");
    }

    [Fact]
    public async Task SetReceiver_Test()
    {
        // without authorized
        {
            var setReceiverRet = await TokenContractStub.SetFeeReceiver.SendWithExceptionAsync(new Address());
            setReceiverRet.TransactionResult.Error.ShouldContain("No permission");
        }

        var methodName = nameof(TokenContractImplContainer.TokenContractImplStub.InitializeFromParentChain);
        var initialInput = new InitializeFromParentChainInput
        {
            Creator = DefaultAddress,
            RegisteredOtherTokenContractAddresses = { { 1, TokenContractAddress } }
        };
        await SubmitAndApproveProposalOfDefaultParliament(TokenContractAddress, methodName, initialInput);
        await TokenContractStub.SetFeeReceiver.SendAsync(DefaultAddress);
        var feeReceiver = await TokenContractStub.GetFeeReceiver.CallAsync(new Empty());
        feeReceiver.Value.ShouldBe(DefaultAddress.Value);
    }

    [Fact]
    public async Task GetFeeCoefficientController_Without_Initialize_Test()
    {
        var userFeeRet = await TokenContractStub.GetUserFeeController
            .SendWithExceptionAsync(new Empty());
        userFeeRet.TransactionResult.Error.ShouldContain(
            "controller does not initialize, call InitializeAuthorizedController first");
        var developerFeeRet = await TokenContractStub.GetDeveloperFeeController
            .SendWithExceptionAsync(new Empty());
        developerFeeRet.TransactionResult.Error.ShouldContain(
            "controller does not initialize, call InitializeAuthorizedController first");
    }

    private async Task SubmitAndApproveProposalOfDefaultParliament(Address contractAddress, string methodName,
        IMessage message)
    {
        var defaultParliamentAddress =
            await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var proposalId = await CreateProposalAsync(contractAddress,
            defaultParliamentAddress, methodName, message);
        await ApproveWithMinersAsync(proposalId);
        var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
        releaseResult.TransactionResult.Error.ShouldBeNullOrEmpty();
        releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    private async Task<IExecutionResult<Empty>> SubmitAndApproveProposalOfDefaultParliamentWithException(
        Address contractAddress,
        string methodName,
        IMessage message)
    {
        var defaultParliamentAddress =
            await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var proposalId = await CreateProposalAsync(contractAddress,
            defaultParliamentAddress, methodName, message);
        await ApproveWithMinersAsync(proposalId);
        return await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId);
    }

    private Transaction GenerateTokenTransaction(Address from, string method, IMessage input)
    {
        return new Transaction
        {
            From = from,
            To = TokenContractAddress,
            MethodName = method,
            Params = ByteString.CopyFrom(input.ToByteArray())
        };
    }
}