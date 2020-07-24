using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using AElf.Contracts.MultiToken;
using AElf.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.FeeCalculation.Extensions;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Shouldly;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests
{
    public sealed class ExecutionPluginForMethodFeeTest : ExecutionPluginForMethodFeeTestBase
    {
        private Address _testContractAddress;
        private TestContract.ContractContainer.ContractStub _testContractStub;
        private ECKeyPair DefaultSenderKeyPair => Accounts[0].KeyPair;
        private Address DefaultSender => Accounts[0].Address;
        
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionSizeFeeSymbolsProvider _transactionSizeFeeSymbolsProvider;
        private readonly IBlockStateSetManger _blockStateSetManger;
        private readonly ITotalTransactionFeesMapProvider _totalTransactionFeesMapProvider;
        
        public ExecutionPluginForMethodFeeTest()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _transactionSizeFeeSymbolsProvider = GetRequiredService<ITransactionSizeFeeSymbolsProvider>();
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
            _totalTransactionFeesMapProvider = GetRequiredService<ITotalTransactionFeesMapProvider>();
        }

        private async Task DeployTestContractAsync()
        {
            var category = KernelConstants.CodeCoverageRunnerCategory;
            var code = Codes.Single(kv => kv.Key.Contains("TestContract")).Value;
            _testContractAddress = await DeploySystemSmartContract(category, code,
                HashHelper.ComputeFrom("TestContract"),
                DefaultSenderKeyPair);
            _testContractStub =
                GetTester<TestContract.ContractContainer.ContractStub>(_testContractAddress, DefaultSenderKeyPair);
        }

        private async Task CreateAndIssueTokenAsync(string symbol, long issueAmount, Address to)
        {
            var tokenStub = await GetTokenContractStubAsync();
            await tokenStub.Create.SendAsync(new CreateInput
            {
                Symbol = symbol,
                Decimals = 2,
                IsBurnable = true,
                TokenName = "test token",
                TotalSupply = 1_000_000_00000000L,
                Issuer = DefaultSender
            });

            if (issueAmount != 0)
            {
                await tokenStub.Issue.SendAsync(new IssueInput
                {
                    Symbol = symbol,
                    Amount = issueAmount,
                    To = to,
                    Memo = "Set for token converter."
                });
            }
        }

        private async Task<TokenContractContainer.TokenContractStub> GetTokenContractStubAsync()
        {
            var tokenContractAddress = await GetTokenContractAddressAsync();
            var tokenStub = GetTester<TokenContractContainer.TokenContractStub>(
                tokenContractAddress, DefaultSenderKeyPair);

            return tokenStub;
        }

        private async Task<Address> GetTokenContractAddressAsync()
        {
            var preBlockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();
            var chainContext = new ChainContext
            {
                BlockHash = preBlockHeader.GetHash(),
                BlockHeight = preBlockHeader.Height
            };
            var contractMapping =
                await ContractAddressService.GetSystemContractNameToAddressMappingAsync(chainContext);

            return contractMapping[TokenSmartContractAddressNameProvider.Name];
        }

        [Fact]
        public async Task GetPreTransactionsTest()
        {
            await DeployTestContractAsync();

            await SetMethodFee_Successful(10);
            var plugins = Application.ServiceProvider.GetRequiredService<IEnumerable<IPreExecutionPlugin>>()
                .ToLookup(p => p.GetType()).Select(coll => coll.First()); // One instance per type
            var plugin = plugins.SingleOrDefault(p => p.GetType() == typeof(FeeChargePreExecutionPlugin));
            plugin.ShouldNotBeNull();
            var bcs = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var chain = await bcs.GetChainAsync();
            var transactions = (await plugin.GetPreTransactionsAsync(TestContract.ContractContainer.Descriptors,
                new TransactionContext
                {
                    Transaction = new Transaction
                    {
                        From = DefaultSender,
                        To = _testContractAddress,
                        MethodName = nameof(_testContractStub.DummyMethod)
                    },
                    BlockHeight = chain.BestChainHeight + 1,
                    PreviousBlockHash = chain.BestChainHash
                })).ToList();

            transactions.ShouldNotBeEmpty();
            transactions[0].From.ShouldBe(DefaultSender);
            transactions[0].To.ShouldBe(await GetTokenContractAddressAsync());
        }

        private async Task SetMethodFee_Successful(long feeAmount)
        {
            await _testContractStub.SetMethodFee.SendAsync(new MethodFees
            {
                MethodName = nameof(_testContractStub.DummyMethod),
                Fees =
                {
                    new MethodFee {Symbol = "ELF", BasicFee = feeAmount}
                }
            });
            var fee = await _testContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(_testContractStub.DummyMethod)
            });
            fee.Fees.First(a => a.Symbol == "ELF").BasicFee.ShouldBe(feeAmount);
        }

        [Fact]
        public async Task ChargeFee_SuccessfulTest()
        {
            await DeployTestContractAsync();

            var feeAmount = 7;
            await SetMethodFee_Successful(feeAmount);

            var tokenContractStub = await GetTokenContractStubAsync();
            var before = await tokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
            {
                Owner = DefaultSender,
                Symbol = "ELF"
            });

            var dummy = await _testContractStub.DummyMethod.SendAsync(new Empty()); // This will deduct the fee
            dummy.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var transactionFeeDic = dummy.TransactionResult.GetChargedTransactionFees();
            await CheckTransactionFeesMapAsync(transactionFeeDic);
           
            var after = await tokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
            {
                Owner = DefaultSender,
                Symbol = "ELF"
            });
            after.Balance.ShouldBe(before.Balance - transactionFeeDic[before.Symbol]);
        }

        private async Task CheckTransactionFeesMapAsync(Dictionary<string, long> transactionFeeDic)
        {
            var chain = await _blockchainService.GetChainAsync();
            var transactionFeesMap = await _totalTransactionFeesMapProvider.GetTotalTransactionFeesMapAsync(new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            });
            foreach (var transactionFee in transactionFeeDic)
            {
                transactionFeesMap.Value[transactionFee.Key].ShouldBe(transactionFee.Value);
            }
        }

        [Fact]
        public async Task ChargeFee_TxFee_FailedTest()
        {
            await DeployTestContractAsync();
            
            var issueAmount = 99999;
            var tokenContractStub = await GetTokenContractStubAsync();
            await tokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = "ELF",
                Amount = issueAmount,
                To = Accounts[1].Address,
                Memo = "Set for token converter."
            });

            var feeAmount = 100000;
            await SetMethodFee_Successful(feeAmount);

            var userTestContractStub =
                GetTester<TestContract.ContractContainer.ContractStub>(_testContractAddress,
                    Accounts[1].KeyPair);
            var dummy = await userTestContractStub.DummyMethod
                .SendWithExceptionAsync(new Empty()); // This will deduct the fee
            dummy.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            dummy.TransactionResult.Error.ShouldBe("Pre-Error: Transaction fee charging failed.");
            var transactionFeeDic = dummy.TransactionResult.GetChargedTransactionFees();
            await CheckTransactionFeesMapAsync(transactionFeeDic);
            
            var afterFee = (await tokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
            {
                Owner = Accounts[1].Address,
                Symbol = "ELF"
            })).Balance;
            afterFee.ShouldBe(0);
            transactionFeeDic["ELF"].ShouldBe(issueAmount);
        }

        [Theory]
        [InlineData(100000000, 0, 3, 10, 1, 2, "ELF", 20260010, true)]
        [InlineData(9, 0, 1, 10, 1, 2, "ELF", 9, false)]
        [InlineData(100000000, 2, 2, 0, 1, 2, "TSA", 1, true)]
        [InlineData(1, 0, 1, 0, 1, 2, "TSB", 1, false)]
        [InlineData(10, 0, 0, 0, 1, 2, "ELF", 10, false)] // Charge 10 ELFs tx size fee.
        public async Task ChargeFeeFailedTests(long balance1, long balance2, long balance3, long fee1, long fee2,
            long fee3, string chargedSymbol, long chargedAmount, bool isChargingSuccessful)
        {
            await DeployTestContractAsync();

            var tokenContractStub = await GetTokenContractStubAsync();
            await tokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = "ELF",
                Amount = balance1,
                To = Accounts[1].Address,
                Memo = "Set for token converter."
            });
            await CreateAndIssueTokenAsync("TSA", balance2,Accounts[1].Address);
            await CreateAndIssueTokenAsync("TSB", balance3,Accounts[1].Address);

            var methodFee = new MethodFees
            {
                MethodName = nameof(_testContractStub.DummyMethod)
            };
            if (fee1 > 0)
                methodFee.Fees.Add(new MethodFee {Symbol = "ELF", BasicFee = fee1});
            if (fee2 > 0)
                methodFee.Fees.Add(new MethodFee {Symbol = "TSA", BasicFee = fee2});
            if (fee3 > 0)
                methodFee.Fees.Add(new MethodFee {Symbol = "TSB", BasicFee = fee3});
            await _testContractStub.SetMethodFee.SendAsync(methodFee);

            var originBalance = (await tokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Accounts[1].Address,
                Symbol = chargedSymbol ?? "ELF"
            })).Balance;

            Dictionary<string,long> transactionFeeDic;
            var userTestContractStub =
                GetTester<TestContract.ContractContainer.ContractStub>(_testContractAddress, Accounts[1].KeyPair);
            if (isChargingSuccessful)
            {
                var dummyResult = await userTestContractStub.DummyMethod.SendAsync(new Empty());
                dummyResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                if (chargedSymbol != null)
                {
                    dummyResult.TransactionResult.GetChargedTransactionFees().Keys.ShouldContain(chargedSymbol);
                    dummyResult.TransactionResult.GetChargedTransactionFees().Values.ShouldContain(chargedAmount);
                }

                transactionFeeDic = dummyResult.TransactionResult.GetChargedTransactionFees();
            }
            else
            {
                var dummyResult = await userTestContractStub.DummyMethod.SendWithExceptionAsync(new Empty());
                dummyResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                dummyResult.TransactionResult.Error.ShouldBe("Pre-Error: Transaction fee charging failed.");
                if (chargedSymbol != null)
                {
                    dummyResult.TransactionResult.GetChargedTransactionFees().Keys.ShouldContain(chargedSymbol);
                }
                transactionFeeDic = dummyResult.TransactionResult.GetChargedTransactionFees();
            }
            await CheckTransactionFeesMapAsync(transactionFeeDic);
            if (chargedSymbol != null)
                transactionFeeDic[chargedSymbol].ShouldBe(chargedAmount);

            var finalBalance = (await tokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Accounts[1].Address,
                Symbol = chargedSymbol ?? "ELF"
            })).Balance;

            (originBalance - finalBalance).ShouldBe(chargedAmount);
        }
        
        [Fact]
        public async Task TransactionSizeFeeSymbolsSetAndGet_Test()
        {
            var blockExecutedDataKey = "BlockExecutedData/TransactionSizeFeeSymbols";
            var chain = await _blockchainService.GetChainAsync();
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(chain.BestChainHash);
            blockStateSet.BlockExecutedData.Keys.ShouldNotContain(blockExecutedDataKey);

            var transactionSizeFeeSymbols = new TransactionSizeFeeSymbols
            {
                TransactionSizeFeeSymbolList =
                {
                    new TransactionSizeFeeSymbol
                    {
                        TokenSymbol = "ELF",
                        AddedTokenWeight = 1,
                        BaseTokenWeight = 1
                    }
                }
            };
            await _transactionSizeFeeSymbolsProvider.SetTransactionSizeFeeSymbolsAsync(new BlockIndex
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            }, transactionSizeFeeSymbols);

            blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(chain.BestChainHash);
            blockStateSet.BlockExecutedData.Keys.ShouldContain(blockExecutedDataKey);

            var symbols = await _transactionSizeFeeSymbolsProvider.GetTransactionSizeFeeSymbolsAsync(
                new ChainContext
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                });
            symbols.ShouldBe(transactionSizeFeeSymbols);
        }
        
                
        [Fact]
        public async Task Method_Fee_Set_Zero_ChargeFee_Should_Be_Zero()
        {
            await DeployTestContractAsync();
            
            await SetMethodFee_Successful(0);

            var tokenContractStub = await GetTokenContractStubAsync();
            var before = await tokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
            {
                Owner = DefaultSender,
                Symbol = "ELF"
            });

            var dummy = await _testContractStub.DummyMethod.SendAsync(new Empty()); // This will deduct the fee
            dummy.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            await _testContractStub.DummyMethod.SendAsync(new Empty());
            
            var after = await tokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
            {
                Owner = DefaultSender,
                Symbol = "ELF"
            });
            before.Balance.ShouldBe(after.Balance);
        }
        
        [Fact]
        public async Task Method_Fee_Not_Set_Zero_ChargeFee()
        {
            await DeployTestContractAsync();

            var tokenContractStub = await GetTokenContractStubAsync();
            var before = await tokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
            {
                Owner = DefaultSender,
                Symbol = "ELF"
            });

            var dummy = await _testContractStub.DummyMethod.SendAsync(new Empty()); // This will deduct the fee
            dummy.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            await _testContractStub.DummyMethod.SendAsync(new Empty());
            
            var after = await tokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
            {
                Owner = DefaultSender,
                Symbol = "ELF"
            });
            before.Balance.ShouldBeGreaterThan(after.Balance);
        }
    }
}