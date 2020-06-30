using System.Threading.Tasks;
using Acs2;
using AElf.ContractTestKit;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.MultiToken
{
    public partial class MultiTokenContractTests
    {
        [Fact]
        public async Task ACS2_GetResourceInfo_Transfer_Test()
        {
            var transaction = GenerateTokenTransaction(Accounts[0].Address, nameof(TokenContractStub.Transfer),
                new TransferInput
                {
                    Amount = 100,
                    Symbol = "ELF",
                    To = Accounts[1].Address,
                    Memo = "Test get resource"
                });
                
            var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
            result.NonParallelizable.ShouldBeFalse();
            result.WritePaths.Count.ShouldBeGreaterThan(0);
        }
        
        [Fact]
        public async Task ACS2_GetResourceInfo_TransferFrom_Test()
        {
            var transaction = GenerateTokenTransaction(Accounts[0].Address, nameof(TokenContractStub.TransferFrom),
                new TransferFromInput
                {
                    Amount = 100,
                    Symbol = "ELF",
                    From = Accounts[1].Address,
                    To = Accounts[2].Address,
                    Memo = "Test get resource"
                });
                
            var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
            result.NonParallelizable.ShouldBeFalse();
            result.WritePaths.Count.ShouldBeGreaterThan(0);
        }
        
        [Fact]
        public async Task ACS2_GetResourceInfo_DonateResourceToken_Test()
        {
            var transaction = GenerateTokenTransaction(Accounts[0].Address, nameof(TokenContractStub.DonateResourceToken),
                new Empty());
                
            var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
            result.NonParallelizable.ShouldBeTrue();
        }
        
        [Fact]
        public async Task ACS2_GetResourceInfo_ClaimTransactionFees_Test()
        {
            var transaction = GenerateTokenTransaction(Accounts[0].Address, nameof(TokenContractStub.ClaimTransactionFees),
                new Empty());
                
            var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
            result.NonParallelizable.ShouldBeTrue();
        }
        
        [Fact]
        public async Task ACS2_GetResourceInfo_UnsupportedMethod_Test()
        {
            var transaction = GenerateTokenTransaction(Accounts[0].Address, "TestMethod",
                new Empty());
                
            var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
            result.ShouldBe(new ResourceInfo {NonParallelizable = true});
        }
        
                [Fact(DisplayName = "[MultiToken] advance token not exist in resource token")]
        public async Task AdvancedResourceToken_Test()
        {
            await CreateNativeTokenAsync();
            long advanceAmount = 1000;
            {
                var tokenNotResrouce = "NORESOURCE";
                await CreateAndIssueCustomizeToken(DefaultAddress, tokenNotResrouce, 10000, 10000);
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
                await CreateAndIssueCustomizeToken(DefaultAddress, trafficToken, 10000, 10000);
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
            await CreateNativeTokenAsync();
            var trafficToken = "TRAFFIC";
            var advanceAmount = 1000;
            await CreateAndIssueCustomizeToken(DefaultAddress, trafficToken, 10000, 10000);
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
            var updateInfo = new UpdateCoefficientsInput
            {
                Coefficients = new CalculateFeeCoefficients
                {
                    FeeTokenType = (int) FeeTypeEnum.Read
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
            var updateInfo = new UpdateCoefficientsInput
            {
                Coefficients = new CalculateFeeCoefficients
                {
                    FeeTokenType = (int) FeeTypeEnum.Tx
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
            setSymbolRet.TransactionResult.Error.ShouldBe("no permission");
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
}