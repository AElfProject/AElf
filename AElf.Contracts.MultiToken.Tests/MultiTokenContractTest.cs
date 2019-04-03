using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Acs1;
using AElf.Common;
using AElf.Contracts.MultiToken.Messages;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Token;
using Google.Protobuf;
using Xunit;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Contracts.MultiToken
{
    public sealed class MultiTokenContractTest : MultiTokenContractTestBase
    {
        private readonly ECKeyPair _spenderKeyPair;
        private Address BasicZeroContractAddress { get; set; }
        private Address TokenContractAddress { get; set; }

        private const string SymbolForTestingInitialLogic = "ELFTEST";
        private const string DefaultSymbol = "ELF";

        private const int DefaultCategory = 3;

        private static long _totalSupply;
        private static long _balanceOfStarter;
    

        public MultiTokenContractTest()
        {
            AsyncHelper.RunSync(() =>
                Tester.InitialChainAsync(Tester.GetDefaultContractTypes(Tester.GetCallOwnerAddress(), out _totalSupply,
                    out _, out _balanceOfStarter)));
            BasicZeroContractAddress = Tester.GetZeroContractAddress();
            TokenContractAddress = Tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
            _spenderKeyPair = CryptoHelpers.GenerateKeyPair();
        }

        [Fact]
        public async Task Deploy_TokenContract()
        {
            var tx = await Tester.GenerateTransactionAsync(BasicZeroContractAddress,
                nameof(ISmartContractZero.DeploySmartContract),
                new ContractDeploymentInput()
                {
                    Category = DefaultCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location))
                });

            await Tester.MineAsync(new List<Transaction> {tx});
            var chain = await Tester.GetChainAsync();
            chain.LongestChainHeight.ShouldBeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task Deploy_TokenContract_Twice()
        {
            var bytes = await Tester.CallContractMethodAsync(BasicZeroContractAddress,
                nameof(ISmartContractZero.DeploySystemSmartContract), new SystemContractDeploymentInput
                {
                    Category = DefaultCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location))
                }
            );
            // Failed to deploy.
            Assert.Empty(bytes);
        }

        [Fact]
        public async Task Initialize_TokenContract()
        {
            var tx = await Tester.GenerateTransactionAsync(TokenContractAddress, nameof(TokenContract.Create),
                new CreateInput
                {
                    Symbol = SymbolForTestingInitialLogic,
                    Decimals = 2,
                    IsBurnable = true,
                    Issuer = Tester.GetCallOwnerAddress(),
                    TokenName = "elf token",
                    TotalSupply = 1000_000L
                });
            var issueTx = await Tester.GenerateTransactionAsync(TokenContractAddress, nameof(TokenContract.Issue),
                new IssueInput
                {
                    Symbol = SymbolForTestingInitialLogic,
                    Amount = 1000_000L,
                    To = Tester.GetCallOwnerAddress(),
                    Memo = "Issue token to starter himself."
                });
            await Tester.MineAsync(new List<Transaction> {tx});
            await Tester.MineAsync(new List<Transaction> {issueTx});
            var result = GetBalanceOutput.Parser.ParseFrom(await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetBalance),
                new GetBalanceInput
                {
                    Symbol = SymbolForTestingInitialLogic,
                    Owner = Tester.GetCallOwnerAddress()
                }));
            result.Balance.ShouldBe(1000_000L);
        }

        [Fact]
        public async Task Initialize_View_TokenContract()
        {
            var tx = await Tester.GenerateTransactionAsync(TokenContractAddress, nameof(TokenContract.Create),
                new CreateInput
                {
                    Symbol = SymbolForTestingInitialLogic,
                    Decimals = 2,
                    IsBurnable = true,
                    Issuer = Address.FromPublicKey(_spenderKeyPair.PublicKey),
                    TokenName = "elf token",
                    TotalSupply = 1000_000L
                });
            await Tester.MineAsync(new List<Transaction> {tx});

            var tokenInfo = TokenInfo.Parser.ParseFrom(await Tester.CallContractMethodAsync(TokenContractAddress, 
                nameof(TokenContract.GetTokenInfo),
                new GetTokenInfoInput
                {
                    Symbol = SymbolForTestingInitialLogic
                }));
            tokenInfo.Symbol.ShouldBe(SymbolForTestingInitialLogic);
            tokenInfo.Decimals.ShouldBe(2);
            tokenInfo.IsBurnable.ShouldBe(true);
            tokenInfo.Issuer.ShouldBe(Address.FromPublicKey(_spenderKeyPair.PublicKey));
            tokenInfo.TokenName.ShouldBe("elf token");
            tokenInfo.TotalSupply.ShouldBe(1000_000L);
        }

        [Fact]
        public async Task Initialize_TokenContract_Failed()
        {
            await Initialize_TokenContract();

            var otherKeyPair = CryptoHelpers.GenerateKeyPair();
            var other = Tester.CreateNewContractTester(otherKeyPair);
            var result = await other.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContract.Create), new CreateInput
                {
                    Symbol = SymbolForTestingInitialLogic,
                    Decimals = 2,
                    IsBurnable = false,
                    Issuer = Address.FromPublicKey(_spenderKeyPair.PublicKey),
                    TokenName = "elf token",
                    TotalSupply = 1000_000L
                });
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Token already exists.").ShouldBeTrue();
        }

        [Fact]
        public async Task Transfer_TokenContract()
        {
            await Initialize_TokenContract();

            var toAddress = CryptoHelpers.GenerateKeyPair();
            await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.Transfer),
                new TransferInput
                {
                    Amount = 1000L,
                    Memo = "transfer test",
                    Symbol = SymbolForTestingInitialLogic,
                    To = Tester.GetAddress(toAddress)
                });

            var result1 = GetBalanceOutput.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetBalance),
                    new GetBalanceInput
                    {
                        Symbol = SymbolForTestingInitialLogic,
                        Owner = Tester.GetCallOwnerAddress()
                    }));
            var result2 = GetBalanceOutput.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetBalance),
                    new GetBalanceInput
                    {
                        Symbol = SymbolForTestingInitialLogic,
                        Owner = Tester.GetAddress(toAddress)
                    }));
            result1.Balance.ShouldBe(_totalSupply - 1000L);
            result2.Balance.ShouldBe(1000L);
        }

        [Fact]
        public async Task Transfer_Without_Enough_Token()
        {
            await Initialize_TokenContract();

            var toAddress = CryptoHelpers.GenerateKeyPair();
            var fromAddress = CryptoHelpers.GenerateKeyPair();
            var from = Tester.CreateNewContractTester(fromAddress);

            var result = from.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.Transfer),
                new TransferInput
                {
                    Amount = 1000L,
                    Memo = "transfer test",
                    Symbol = SymbolForTestingInitialLogic,
                    To = from.GetAddress(toAddress)
                });
            result.Result.Status.ShouldBe(TransactionResultStatus.Failed);
            var balance =GetBalanceOutput.Parser.ParseFrom(await from.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetBalance),
                new GetBalanceInput
                {
                    Owner = from.GetAddress(fromAddress),
                    Symbol = DefaultSymbol
                })).Balance;
            balance.ShouldBe(0L);
            result.Result.Error.Contains($"Insufficient balance").ShouldBeTrue();
        }

        [Fact]
        public async Task Approve_TokenContract()
        {
            await Initialize_TokenContract();

            var owner = Tester.GetCallOwnerAddress();
            var spender = Tester.GetAddress(_spenderKeyPair);

            var result1 = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContract.Approve), new ApproveInput
                {
                    Symbol = DefaultSymbol,
                    Amount = 2000L,
                    Spender = spender
                });
            result1.Status.ShouldBe(TransactionResultStatus.Mined);
            var allowanceOutput =GetAllowanceOutput.Parser.ParseFrom(await Tester.CallContractMethodAsync(TokenContractAddress, 
                nameof(TokenContract.GetAllowance),
                new GetAllowanceInput
                {
                    Owner = owner,
                    Spender = spender,
                    Symbol = DefaultSymbol
                }));
            allowanceOutput.Allowance.ShouldBe(2000L);
        }

        [Fact]
        public async Task UnApprove_TokenContract()
        {
            await Approve_TokenContract();
            var owner = Tester.GetCallOwnerAddress();
            var spender = Tester.GetAddress(_spenderKeyPair);

            var result2 =
                await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.UnApprove),
                    new UnApproveInput
                    {
                        Amount = 1000L,
                        Symbol = DefaultSymbol,
                        Spender = spender
                    });
            result2.Status.ShouldBe(TransactionResultStatus.Mined);
            var allowanceOutput = GetAllowanceOutput.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetAllowance),
                new GetAllowanceInput
                {
                    Owner = owner,
                    Spender = spender,
                    Symbol = DefaultSymbol
                }));
            allowanceOutput.Allowance.ShouldBe(2000L - 1000L);
        }

        [Fact]
        public async Task UnApprove_Without_Enough_Allowance()
        {
            await Initialize_TokenContract();

            var owner = Tester.GetCallOwnerAddress();
            var spender = Tester.GetAddress(_spenderKeyPair);

            var allowanceOutput = GetAllowanceOutput.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetAllowance),
                    new GetAllowanceInput
                    {
                        Owner = owner,
                        Spender = spender,
                        Symbol = DefaultSymbol
                    }));
            allowanceOutput.Allowance.ShouldBe(0L);
            var result = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContract.UnApprove), new UnApproveInput
                {
                    Amount = 1000L,
                    Spender = spender,
                    Symbol = SymbolForTestingInitialLogic
                });
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task TransferFrom_TokenContract()
        {
            await Approve_TokenContract();

            var owner = Tester.GetCallOwnerAddress();
            var spenderAddress = Tester.GetAddress(_spenderKeyPair);

            var spender = Tester.CreateNewContractTester(_spenderKeyPair);
            var result2 =
                await spender.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.TransferFrom),
                    new TransferFromInput
                    {
                        Amount = 1000L,
                        From = owner,
                        Memo = "test",
                        Symbol = DefaultSymbol,
                        To = spenderAddress
                    });
            result2.Status.ShouldBe(TransactionResultStatus.Mined);
            var allowanceOutput2 = GetAllowanceOutput.Parser.ParseFrom(
                await spender.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetAllowance),
                new GetAllowanceInput
                {
                    Owner = owner,
                    Spender = spenderAddress,
                    Symbol = DefaultSymbol,
                }));
            allowanceOutput2.Allowance.ShouldBe(2000L - 1000L);

            var allowanceOutput3 = GetBalanceOutput.Parser.ParseFrom(
                await spender.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetBalance),
                    new GetBalanceInput
                    {
                        Owner = spenderAddress,
                        Symbol = DefaultSymbol
                    }));
            allowanceOutput3.Balance.ShouldBe(1000L);
        }

        [Fact]
        public async Task TransferFrom_With_ErrorAccount()
        {
            await Approve_TokenContract();

            var owner = Tester.GetCallOwnerAddress();
            var spender = Tester.GetAddress(_spenderKeyPair);

            var result2 =
                await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.TransferFrom),
                    new TransferFromInput
                    {
                        Amount = 1000L,
                        From = owner,
                        Memo = "transfer from test",
                        Symbol = DefaultSymbol,
                        To = spender
                    });
            result2.Status.ShouldBe(TransactionResultStatus.Failed);
            result2.Error.Contains("Insufficient allowance.").ShouldBeTrue();

            var allowanceOutput2 = GetAllowanceOutput.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetAllowance),
                    new GetAllowanceInput
                    {
                        Owner = owner,
                        Spender = spender,
                        Symbol = DefaultSymbol
                    }));
            allowanceOutput2.Allowance.ShouldBe(2000L);

            var balanceOutput3 = GetBalanceOutput.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetBalance),
                    new GetBalanceInput
                    {
                        Owner = spender,
                        Symbol = DefaultSymbol
                    }));
            balanceOutput3.Balance.ShouldBe(0L);
        }

        [Fact]
        public async Task TransferFrom_Without_Enough_Allowance()
        {
            await Initialize_TokenContract();
            var owner = Tester.GetCallOwnerAddress();
            var spender = Tester.GetAddress(_spenderKeyPair);

            var allowanceOutput = GetAllowanceOutput.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetAllowance),
                    new GetAllowanceInput
                    {
                        Owner = owner,
                        Spender = spender,
                        Symbol = DefaultSymbol
                    }));
            allowanceOutput.Allowance.ShouldBe(0L);

            //Tester.SetCallOwner(spenderKeyPair);
            var result =
                await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, 
                    nameof(TokenContract.TransferFrom),
                    new TransferFromInput
                    {
                        Amount = 1000L,
                        From = owner,
                        Memo = "transfer from test",
                        Symbol = DefaultSymbol,
                        To = spender
                    });
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Insufficient allowance.").ShouldBeTrue();
        }

        [Fact]
        public async Task Burn_TokenContract()
        {
            await Initialize_TokenContract();
            await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.Burn), new BurnInput
            {
                Amount = 3000L,
                Symbol = DefaultSymbol
            });
            var balance = GetBalanceOutput.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetBalance),
                    new GetBalanceInput
                    {
                        Owner = Tester.GetCallOwnerAddress(),
                        Symbol = DefaultSymbol
                    }));
            balance.Balance.ShouldBe(_balanceOfStarter - 3000L);
        }

        [Fact]
        public async Task Burn_Without_Enough_Balance()
        {
            await Initialize_TokenContract();
            var burnerAddress = CryptoHelpers.GenerateKeyPair();
            var burner = Tester.CreateNewContractTester(burnerAddress);
            var result = await burner.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.Burn),
                new BurnInput
                {
                    Symbol = DefaultSymbol,
                    Amount = 3000L
                });
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Burner doesn't own enough balance.").ShouldBeTrue();
        }

        [Fact]
        public async Task Charge_Transaction_Fees()
        {
            await Initialize_TokenContract();

            var result =
                await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
                    nameof(TokenContract.ChargeTransactionFees), new ChargeTransactionFeesInput
                    {
                        Amount = 10L,
                        Symbol = DefaultSymbol
                    });
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.Transfer),
                new TransferInput
                {
                    Symbol = DefaultSymbol,
                    Amount = 1000L,
                    Memo = "transfer test",
                    To = Tester.GetAddress(_spenderKeyPair)
                });
            var balanceOutput = GetBalanceOutput.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetBalance),
                    new GetBalanceInput
                    {
                        Owner = Tester.GetCallOwnerAddress(),
                        Symbol = DefaultSymbol
                    }));
            balanceOutput.Balance.ShouldBe(_balanceOfStarter - 1000L - 10L);
        }

        [Fact]
        public async Task Claim_Transaction_Fees_Without_FeePoolAddress()
        {
            await Initialize_TokenContract();
            var result = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContract.ClaimTransactionFees), new ClaimTransactionFeesInput
                {
                    Symbol = DefaultSymbol,
                    Height = 1L
                });
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Fee pool address is not set.").ShouldBeTrue();
        }

        [Fact]
        public async Task Set_And_Get_Method_Fee()
        {
            await Initialize_TokenContract();

            {
                var resultGetBytes = await Tester.CallContractMethodAsync(TokenContractAddress,
                    nameof(TokenContract.GetMethodFee), new GetMethodFeeInput
                    {
                        Method = nameof(TokenContract.Transfer)
                    });
                var resultGet = GetMethodFeeOutput.Parser.ParseFrom(resultGetBytes);
                resultGet.Fee.ShouldBe(0L);
            }

            var resultSet = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContract.SetMethodFee), new SetMethodFeeInput
                {
                    Method = nameof(TokenContract.Transfer),
                    Fee = 10L
                });
            resultSet.Status.ShouldBe(TransactionResultStatus.Mined);

            {
                var resultGetBytes = await Tester.CallContractMethodAsync(TokenContractAddress,
                    nameof(TokenContract.GetMethodFee), new GetMethodFeeInput
                    {
                        Method = nameof(TokenContract.Transfer)
                    });
                var resultGet = GetMethodFeeOutput.Parser.ParseFrom(resultGetBytes);
                resultGet.Fee.ShouldBe(10L);
            }
        }

        [Fact]
        public async Task Set_FeePoolAddress()
        {
            await Initialize_TokenContract();

            var transactionResult = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContract.SetFeePoolAddress),
                DividendsSmartContractAddressNameProvider.Name);
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //set again
            transactionResult = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContract.SetFeePoolAddress),
                DividendsSmartContractAddressNameProvider.Name);
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Fee pool address already set.").ShouldBeTrue();
        }
    }
}
