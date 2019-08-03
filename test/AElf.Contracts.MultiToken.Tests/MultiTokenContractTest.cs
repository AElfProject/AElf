using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acs1;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.CSharp.Core.Utils;
using AElf.Kernel;
using AElf.Types;
using Xunit;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Contracts.MultiToken
{
    public sealed class MultiTokenContractTest : MultiTokenContractTestBase
    {
        private const string SymbolForTestingInitialLogic = "ELFTEST";

        private static long _totalSupply = 1_000_000L;
        private static long _balanceOfStarter = 800_000L;

        public MultiTokenContractTest()
        {
            AsyncHelper.RunSync(async () => await InitializeAsync());
        }

        private async Task InitializeAsync()
        {
            {
                // TokenContract
                var category = KernelConstants.CodeCoverageRunnerCategory;
                var code = TokenContractCode;
                TokenContractAddress = await DeployContractAsync(category, code, Hash.FromString("MultiToken"), DefaultSenderKeyPair);
                TokenContractStub =
                    GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultSenderKeyPair);

                await TokenContractStub.CreateNativeToken.SendAsync(new CreateNativeTokenInput()
                {
                    Symbol = DefaultSymbol,
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "elf token",
                    TotalSupply = _totalSupply,
                    Issuer = DefaultSender
                });
                await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = DefaultSymbol,
                    Amount = _balanceOfStarter,
                    To = DefaultSender,
                    Memo = "Set for token converter."
                });
            }
        }

        [Fact]
        public async Task InitializeTokenContract_Test()
        {
            await TokenContractStub.Create.SendAsync(
                new CreateInput
                {
                    Symbol = SymbolForTestingInitialLogic,
                    Decimals = 2,
                    IsBurnable = true,
                    Issuer = DefaultSender,
                    TokenName = "elf token",
                    TotalSupply = 1000_000L
                });
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = SymbolForTestingInitialLogic,
                Amount = 1000_000L,
                To = DefaultSender,
                Memo = "Issue token to starter himself."
            });
            var result = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = SymbolForTestingInitialLogic,
                Owner = DefaultSender
            });
            result.Balance.ShouldBe(1000_000L);
        }

        [Fact]
        public async Task ViewTokenContract_Test()
        {
            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = SymbolForTestingInitialLogic,
                Decimals = 2,
                IsBurnable = true,
                Issuer = User1Address,
                TokenName = "elf token",
                TotalSupply = 1000_000L
            });

            var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = SymbolForTestingInitialLogic
            });
            tokenInfo.Symbol.ShouldBe(SymbolForTestingInitialLogic);
            tokenInfo.Decimals.ShouldBe(2);
            tokenInfo.IsBurnable.ShouldBe(true);
            tokenInfo.Issuer.ShouldBe(User1Address);
            tokenInfo.TokenName.ShouldBe("elf token");
            tokenInfo.TotalSupply.ShouldBe(1000_000L);
        }

        [Fact]
        public async Task Initialize_TokenContract_FailedTest()
        {
            await InitializeTokenContract_Test();
            var anotherStub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);

            var result = (await anotherStub.Create.SendAsync(new CreateInput
            {
                Symbol = SymbolForTestingInitialLogic,
                Decimals = 2,
                IsBurnable = false,
                Issuer = User1Address,
                TokenName = "elf token",
                TotalSupply = 1000_000L
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Token already exists.").ShouldBeTrue();
        }

        [Fact]
        public async Task TransferToken_Test()
        {
            await InitializeTokenContract_Test();
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                Amount = 1000L,
                Memo = "transfer test",
                Symbol = SymbolForTestingInitialLogic,
                To = User1Address
            });

            var result1 = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = SymbolForTestingInitialLogic,
                Owner = DefaultSender
            });
            var result2 = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = SymbolForTestingInitialLogic,
                Owner = User1Address
            });

            result1.Balance.ShouldBe(_totalSupply - 1000L);
            result2.Balance.ShouldBe(1000L);
        }

        [Fact]
        public async Task Transfer_WithoutEnoughToken_Test()
        {
            var anotherStub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);
            var result = (await anotherStub.Transfer.SendAsync(new TransferInput
            {
                Amount = 1000L,
                Memo = "transfer test",
                Symbol = DefaultSymbol,
                To = User2Address
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains($"Insufficient balance").ShouldBeTrue();
        }

        [Fact]
        public async Task ApproveToken_Test()
        {
            var result1 = (await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = DefaultSymbol,
                Amount = 2000L,
                Spender = User1Address
            })).TransactionResult;

            result1.Status.ShouldBe(TransactionResultStatus.Mined);
            var allowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = DefaultSender,
                Spender = User1Address,
                Symbol = DefaultSymbol
            });
            allowanceOutput.Allowance.ShouldBe(2000L);
        }

        [Fact]
        public async Task UnApproveToken_Test()
        {
            await ApproveToken_Test();
            var result2 = (await TokenContractStub.UnApprove.SendAsync(new UnApproveInput
            {
                Amount = 1000L,
                Symbol = DefaultSymbol,
                Spender = User1Address
            })).TransactionResult;

            result2.Status.ShouldBe(TransactionResultStatus.Mined);
            var allowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = DefaultSender,
                Spender = User1Address,
                Symbol = DefaultSymbol
            });
            allowanceOutput.Allowance.ShouldBe(2000L - 1000L);
        }

        [Fact]
        public async Task UnApproveToken_WithoutEnoughAllowance_Test()
        {
            var allowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = DefaultSender,
                Spender = User1Address,
                Symbol = DefaultSymbol
            });

            allowanceOutput.Allowance.ShouldBe(0L);
            var result = (await TokenContractStub.UnApprove.SendAsync(new UnApproveInput()
            {
                Amount = 1000L,
                Spender = User1Address,
                Symbol = DefaultSymbol
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task TransferFromToken_Test()
        {
            await ApproveToken_Test();
            var user1Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);
            var result2 = await user1Stub.TransferFrom.SendAsync(new TransferFromInput
            {
                Amount = 1000L,
                From = DefaultSender,
                Memo = "test",
                Symbol = DefaultSymbol,
                To = User1Address
            });
            result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var allowanceOutput2 =
                await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
                {
                    Owner = DefaultSender,
                    Spender = User1Address,
                    Symbol = DefaultSymbol,
                });
            allowanceOutput2.Allowance.ShouldBe(2000L - 1000L);

            var allowanceOutput3 = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = User1Address,
                Symbol = DefaultSymbol
            });
            allowanceOutput3.Balance.ShouldBe(1000L);
        }

        [Fact]
        public async Task TransferFromToken_WithErrorAccount_Test()
        {
            await ApproveToken_Test();
            var result2 = (await TokenContractStub.TransferFrom.SendAsync(new TransferFromInput
            {
                Amount = 1000L,
                From = DefaultSender,
                Memo = "transfer from test",
                Symbol = DefaultSymbol,
                To = User1Address
            })).TransactionResult;
            result2.Status.ShouldBe(TransactionResultStatus.Failed);
            result2.Error.Contains("Insufficient allowance.").ShouldBeTrue();

            var allowanceOutput2 = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = DefaultSender,
                Spender = User1Address,
                Symbol = DefaultSymbol
            });
            allowanceOutput2.Allowance.ShouldBe(2000L);

            var balanceOutput3 = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = User1Address,
                Symbol = DefaultSymbol
            });
            balanceOutput3.Balance.ShouldBe(0L);
        }

        [Fact]
        public async Task TransferFromToken_WithoutEnoughAllowance_Test()
        {
            var allowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = DefaultSender,
                Spender = User1Address,
                Symbol = DefaultSymbol
            });
            allowanceOutput.Allowance.ShouldBe(0L);
            var user1Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);
            var result = (await user1Stub.TransferFrom.SendAsync(new TransferFromInput
            {
                Amount = 1000L,
                From = DefaultSender,
                Memo = "transfer from test",
                Symbol = DefaultSymbol,
                To = User1Address
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Insufficient allowance.").ShouldBeTrue();
        }

        [Fact]
        public async Task BurnToken_Test()
        { 
            await TokenContractStub.Burn.SendAsync(new BurnInput
            {
                Amount = 3000L,
                Symbol = DefaultSymbol
            });
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultSender,
                Symbol = DefaultSymbol
            });
            balance.Balance.ShouldBe(_balanceOfStarter - 3000L);
        }

        [Fact]
        public async Task BurnToken_WithoutEnoughBalance_Test()
        {
            var user1Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);
            var result = (await user1Stub.Burn.SendAsync(new BurnInput
            {
                Symbol = DefaultSymbol,
                Amount = 3000L
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Burner doesn't own enough balance.").ShouldBeTrue();
        }

        [Fact]
        public async Task ChargeTransactionFees_Test()
        {
            var result = (await TokenContractStub.ChargeTransactionFees.SendAsync(new ChargeTransactionFeesInput
            {
                Amount = 10L,
                Symbol = DefaultSymbol
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                Symbol = DefaultSymbol,
                Amount = 1000L,
                Memo = "transfer test",
                To = User1Address
            });
            var balanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultSender,
                Symbol = DefaultSymbol
            });
            balanceOutput.Balance.ShouldBe(_balanceOfStarter - 1000L - 10L);
        }

        [Fact]
        public async Task ClaimTransactionFees_WithoutFeePoolAddress_Test()
        {
            var result = (await TokenContractStub.ClaimTransactionFees.SendAsync(new ClaimTransactionFeesInput
            {
                Symbol = DefaultSymbol,
                Height = 1L
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Fee pool address is not set.").ShouldBeTrue();
        }

        [Fact]
        public async Task SetAndGetMethodFee_Test()
        {
            var feeChargerStub = GetTester<FeeChargedContractContainer.FeeChargedContractStub>(TokenContractAddress,
                DefaultSenderKeyPair);
            {
                var fee = await feeChargerStub.GetMethodFee.CallAsync(
                    new MethodName()
                    {
                        Name = nameof(TokenContractContainer.TokenContractStub.Transfer)
                    });
                fee.Amount.ShouldBe(0L);
            }

            var resultSet = (await feeChargerStub.SetMethodFee.SendAsync(new SetMethodFeeInput
            {
                Method = nameof(TokenContractContainer.TokenContractStub.Transfer),
                Symbol = DefaultSymbol,
                Amount = 10L
            })).TransactionResult; 
            resultSet.Status.ShouldBe(TransactionResultStatus.Mined);

            {
                var fee = await feeChargerStub.GetMethodFee.CallAsync(
                    new MethodName()
                    {
                        Name = nameof(TokenContractContainer.TokenContractStub.Transfer)
                    });
                fee.Amount.ShouldBe(10L);
            }
        }

        [Fact(Skip = "Failed because we didn't deploy election contract in test base for now.")]
        public async Task Set_FeePoolAddress()
        {
            // this is needed, NOT GOOD DESIGN, it doesn't matter what code we deploy, all we need is an address
            await DeploySystemSmartContract(KernelConstants.CodeCoverageRunnerCategory, TokenContractCode, DividendSmartContractAddressNameProvider.Name,
                DefaultSenderKeyPair);

            var transactionResult =
                (await TokenContractStub.SetFeePoolAddress.SendAsync(DividendSmartContractAddressNameProvider.Name))
                .TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //set again
            transactionResult = (await TokenContractStub.SetFeePoolAddress.SendAsync(DividendSmartContractAddressNameProvider.Name))
                .TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Fee pool address already set.").ShouldBeTrue();
        }
    }
}