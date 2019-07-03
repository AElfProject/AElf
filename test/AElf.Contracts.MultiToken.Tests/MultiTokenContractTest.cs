using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acs1;
using Acs5;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.TestContract.MethodCallThreshold;
using AElf.Contracts.Treasury;
using AElf.Kernel;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using Shouldly;
using Volo.Abp.Threading;
using AElf.Contracts.TokenConverter;
using AElf.Cryptography;
using AElf.Kernel.Token;

namespace AElf.Contracts.MultiToken
{
    public sealed class MultiTokenContractTest : MultiTokenContractTestBase
    {
        private const string SymbolForTestingInitialLogic = "ELFTEST";

        private static long _totalSupply = 1_000_000L;
        private static long _balanceOfStarter = 800_000L;

        private Connector RamConnector = new Connector
        {
            Symbol = "AETC",
            VirtualBalance = 0,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = false
        };

        public MultiTokenContractTest()
        {
            //AsyncHelper.RunSync(async () => await InitializeAsync());
            DeployContracts();
            InitialContracts();
        }

        private async Task InitializeAsync()
        {
            {
                // TokenContract
                var category = KernelConstants.CodeCoverageRunnerCategory;
                var code = TokenContractCode;
                TokenContractAddress = await DeploySystemSmartContract(category, code,
                    TokenSmartContractAddressNameProvider.Name, DefaultKeyPair);
                TokenContractStub =
                    GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultKeyPair);
            }
        }

        private void DeployContracts()
        {
            var category = KernelConstants.CodeCoverageRunnerCategory;

            // ProfitContract
            {
                var code = ProfitContractCode;
                ProfitContractAddress = AsyncHelper.RunSync(() =>
                    DeploySystemSmartContract(category, code, ProfitSmartContractAddressNameProvider.Name,
                        DefaultKeyPair)
                );
                ProfitContractStub =
                    GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress,
                        DefaultKeyPair);
            }

            // TreasuryContract
            {
                var code = TreasuryContractCode;
                TreasuryContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(category, code,
                    TreasurySmartContractAddressNameProvider.Name, DefaultKeyPair));
                TreasuryContractStub =
                    GetTester<TreasuryContractContainer.TreasuryContractStub>(TreasuryContractAddress,
                        DefaultKeyPair);
            }

            // TokenContract
            {
                var code = TokenContractCode;
                TokenContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(category, code,
                    TokenSmartContractAddressNameProvider.Name, DefaultKeyPair));
                TokenContractStub =
                    GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultKeyPair);
            }

            //TokenConvertContract
            {
                var code = TokenConverterContractCode;
                TokenConverterContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(category, code,
                    TokenConverterSmartContractAddressNameProvider.Name, DefaultKeyPair));
                TokenConverterContractStub =
                    GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterContractAddress,
                        DefaultKeyPair);
            }
        }

        private void InitialContracts()
        {
            {
                TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = DefaultSymbol,
                    TokenName = "Native Token",
                    TotalSupply = _totalSupply,
                    Decimals = 2,
                    IsBurnable = true,
                    Issuer = DefaultAddress,
                    LockWhiteList =
                    {
                        ProfitContractAddress,
                        TreasuryContractAddress
                    }
                });
                TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = "AETC",
                    TokenName = "AElf Token Converter Token",
                    TotalSupply = 500_000L,
                    Decimals = 2,
                    Issuer = DefaultAddress,
                    IsBurnable = true,
                    LockWhiteList =
                    {
                        ProfitContractAddress,
                        TreasuryContractAddress
                    }
                });
            }

            {
                var result = AsyncHelper.RunSync(() => TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = DefaultSymbol,
                    Amount = _balanceOfStarter,
                    To = DefaultAddress,
                    Memo = "Set for token converter."
                }));
                CheckResult(result.TransactionResult);
            }

            {
                var result = AsyncHelper.RunSync(() => TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = DefaultSymbol,
                    Amount = _totalSupply - _balanceOfStarter,
                    To = TokenContractAddress,
                    Memo = "Set for token converter."
                }));
                CheckResult(result.TransactionResult);
            }

            {
                var result = AsyncHelper.RunSync(() => ProfitContractStub.CreateProfitItem.SendAsync(
                    new CreateProfitItemInput
                    {
                        ProfitReceivingDuePeriodCount = 10
                    }));
                CheckResult(result.TransactionResult);
            }

            {
                var result = AsyncHelper.RunSync(() =>
                    TokenConverterContractStub.Initialize.SendAsync(new InitializeInput
                    {
                        BaseTokenSymbol = DefaultSymbol,
                        FeeRate = "0.005",
                        ManagerAddress = ManagerAddress,
                        TokenContractAddress = TokenContractAddress,
                        FeeReceiverAddress = ManagerAddress,
                        Connectors = {RamConnector}
                    }));
                CheckResult(result.TransactionResult);
            }

            {
                var result =
                    AsyncHelper.RunSync(() =>
                        TreasuryContractStub.InitialTreasuryContract.SendAsync(new InitialTreasuryContractInput()));
                CheckResult(result.TransactionResult);
            }
        }


        private void CheckResult(TransactionResult result)
        {
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new Exception(result.Error);
            }
        }

        [Fact]
        public async Task Issue_Token_MultipleTimes()
        {
            // TokenContract
            var category = KernelConstants.CodeCoverageRunnerCategory;
            var code = TokenContractCode;
            TokenContractAddress = await DeployContractAsync(category, code, DefaultKeyPair);
            TokenContractStub =
                GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultKeyPair);

            var createResult = await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = DefaultSymbol,
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = _totalSupply,
                Issuer = DefaultAddress
            });
            createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //first issue
            {
                var issueResult1 = await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = DefaultSymbol,
                    Amount = 1000,
                    To = DefaultAddress,
                    Memo = "first issue token."
                });
                issueResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = DefaultAddress,
                    Symbol = DefaultSymbol
                })).Balance;
                balance.ShouldBe(1000);
            }

            //second issue
            {
                var issueResult1 = await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = DefaultSymbol,
                    Amount = 1000,
                    To = DefaultAddress,
                    Memo = "second issue token."
                });
                issueResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = DefaultAddress,
                    Symbol = DefaultSymbol
                })).Balance;
                balance.ShouldBe(2000);
            }
        }

        [Fact]
        public async Task Initialize_TokenContract()
        {
            await TokenContractStub.Create.SendAsync(
                new CreateInput
                {
                    Symbol = SymbolForTestingInitialLogic,
                    Decimals = 2,
                    IsBurnable = true,
                    Issuer = DefaultAddress,
                    TokenName = "elf token",
                    TotalSupply = 1000_000L
                });
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = SymbolForTestingInitialLogic,
                Amount = 1000_000L,
                To = DefaultAddress,
                Memo = "Issue token to starter himself."
            });
            var result = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = SymbolForTestingInitialLogic,
                Owner = DefaultAddress
            });
            result.Balance.ShouldBe(1000_000L);
        }

        [Fact]
        public async Task Initialize_View_TokenContract()
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
        public async Task Initialize_TokenContract_Failed()
        {
            await Initialize_TokenContract();

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
        public async Task Transfer_TokenContract()
        {
            await Initialize_TokenContract();

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
                Owner = DefaultAddress
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
        public async Task Transfer_Without_Enough_Token()
        {
            await Initialize_TokenContract();

            var anotherStub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);
            var result = (await anotherStub.Transfer.SendAsync(new TransferInput
            {
                Amount = 1000L,
                Memo = "transfer test",
                Symbol = SymbolForTestingInitialLogic,
                To = User2Address
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains($"Insufficient balance").ShouldBeTrue();
        }

        [Fact]
        public async Task Approve_TokenContract()
        {
            await Initialize_TokenContract();

            var result1 = (await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = DefaultSymbol,
                Amount = 2000L,
                Spender = User1Address
            })).TransactionResult;

            result1.Status.ShouldBe(TransactionResultStatus.Mined);
            var allowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = DefaultAddress,
                Spender = User1Address,
                Symbol = DefaultSymbol
            });
            allowanceOutput.Allowance.ShouldBe(2000L);
        }

        [Fact]
        public async Task UnApprove_TokenContract()
        {
            await Approve_TokenContract();
            var result2 = (await TokenContractStub.UnApprove.SendAsync(new UnApproveInput
            {
                Amount = 1000L,
                Symbol = DefaultSymbol,
                Spender = User1Address
            })).TransactionResult;

            result2.Status.ShouldBe(TransactionResultStatus.Mined);
            var allowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = DefaultAddress,
                Spender = User1Address,
                Symbol = DefaultSymbol
            });
            allowanceOutput.Allowance.ShouldBe(2000L - 1000L);
        }

        [Fact]
        public async Task UnApprove_Without_Enough_Allowance()
        {
            await Initialize_TokenContract();

            var allowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = DefaultAddress,
                Spender = User1Address,
                Symbol = DefaultSymbol
            });

            allowanceOutput.Allowance.ShouldBe(0L);
            var result = (await TokenContractStub.UnApprove.SendAsync(new UnApproveInput()
            {
                Amount = 1000L,
                Spender = User1Address,
                Symbol = SymbolForTestingInitialLogic
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task TransferFrom_TokenContract()
        {
            await Approve_TokenContract();
            var user1Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);
            var result2 = await user1Stub.TransferFrom.SendAsync(new TransferFromInput
            {
                Amount = 1000L,
                From = DefaultAddress,
                Memo = "test",
                Symbol = DefaultSymbol,
                To = User1Address
            });
            result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var allowanceOutput2 =
                await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
                {
                    Owner = DefaultAddress,
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
        public async Task TransferFrom_With_ErrorAccount()
        {
            await Approve_TokenContract();
            var result2 = (await TokenContractStub.TransferFrom.SendAsync(new TransferFromInput
            {
                Amount = 1000L,
                From = DefaultAddress,
                Memo = "transfer from test",
                Symbol = DefaultSymbol,
                To = User1Address
            })).TransactionResult;
            result2.Status.ShouldBe(TransactionResultStatus.Failed);
            result2.Error.Contains("Insufficient allowance.").ShouldBeTrue();

            var allowanceOutput2 = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = DefaultAddress,
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
        public async Task TransferFrom_Without_Enough_Allowance()
        {
            await Initialize_TokenContract();
            var allowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = DefaultAddress,
                Spender = User1Address,
                Symbol = DefaultSymbol
            });
            allowanceOutput.Allowance.ShouldBe(0L);
            var user1Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);
            var result = (await user1Stub.TransferFrom.SendAsync(new TransferFromInput
            {
                Amount = 1000L,
                From = DefaultAddress,
                Memo = "transfer from test",
                Symbol = DefaultSymbol,
                To = User1Address
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Insufficient allowance.").ShouldBeTrue();
        }

        [Fact]
        public async Task Burn_TokenContract()
        {
            await Initialize_TokenContract();
            await TokenContractStub.Burn.SendAsync(new BurnInput
            {
                Amount = 3000L,
                Symbol = DefaultSymbol
            });
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultAddress,
                Symbol = DefaultSymbol
            });
            balance.Balance.ShouldBe(_balanceOfStarter - 3000L);
        }

        [Fact]
        public async Task Burn_Without_Enough_Balance()
        {
            await Initialize_TokenContract();
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
        public async Task Charge_Transaction_Fees()
        {
            //await Initialize_TokenContract();

            var result = (await TokenContractStub.ChargeTransactionFees.SendAsync(new ChargeTransactionFeesInput
            {
                SymbolToAmount = {new Dictionary<string, long> {{DefaultSymbol, 10L}}}
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
                Owner = DefaultAddress,
                Symbol = DefaultSymbol
            });
            balanceOutput.Balance.ShouldBe(_balanceOfStarter - 1000L - 10L);
        }

        [Fact]
        public async Task Claim_Transaction_Fees()
        {
            var originBalanceOutput1 = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TreasuryContractAddress,
                Symbol = DefaultSymbol
            });
            originBalanceOutput1.Balance.ShouldBe(0L);
            await Charge_Transaction_Fees();

            var originBalanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TreasuryContractAddress,
                Symbol = DefaultSymbol
            });
            originBalanceOutput.Balance.ShouldBe(10L);

            {
                var result = (await TokenContractStub.ClaimTransactionFees.SendAsync(new Empty()
                )).TransactionResult;
                CheckResult(result);
            }

            var balanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TreasuryContractAddress,
                Symbol = DefaultSymbol
            });
            balanceOutput.Balance.ShouldBe(10L);

        }

        [Fact]
        public async Task Set_And_Get_Method_Fee()
        {
            await Initialize_TokenContract();
            var feeChargerStub = GetTester<FeeChargedContractContainer.FeeChargedContractStub>(TokenContractAddress,
                DefaultKeyPair);

            // Fee not set yet.
            {
                var fee = await feeChargerStub.GetMethodFee.CallAsync(
                    new MethodName
                    {
                        Name = nameof(TokenContractContainer.TokenContractStub.Transfer)
                    });
                fee.SymbolToAmount.Keys.ShouldNotContain(DefaultSymbol);
            }

            // Set method fee.
            var resultSet = (await feeChargerStub.SetMethodFee.SendAsync(new SetMethodFeeInput
            {
                Method = nameof(TokenContractContainer.TokenContractStub.Transfer),
                SymbolToAmount = {new Dictionary<string, long> {{DefaultSymbol, 10L}}}
            })).TransactionResult;
            resultSet.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check fee.
            {
                var fee = await feeChargerStub.GetMethodFee.CallAsync(
                    new MethodName()
                    {
                        Name = nameof(TokenContractContainer.TokenContractStub.Transfer)
                    });
                fee.SymbolToAmount[DefaultSymbol].ShouldBe(10L);
            }
        }

    }
}