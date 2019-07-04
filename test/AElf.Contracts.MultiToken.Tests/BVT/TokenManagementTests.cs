using System.Threading.Tasks;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.TestKit;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Kernel;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Token;
using AElf.Sdk.CSharp;
using AElf.Types;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

// ReSharper disable CheckNamespace
namespace AElf.Contracts.MultiToken
{
    public partial class MultiTokenContractTests : MultiTokenContractTestBase
    {
        
        private Connector RamConnector = new Connector
        {
            Symbol = "AETC",
            VirtualBalance = 0,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = false
        };
        /// <summary>
        /// Burnable & Transferable
        /// </summary>
        private TokenInfo AliceCoinTokenInfo { get; set; } = new TokenInfo
        {
            Symbol = "ALICE",
            TokenName = "For testing multi-token contract",
            TotalSupply = 1_000_000_000_00000000,
            Decimals = 8,
            IsBurnable = true,
            Issuer = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[0].PublicKey),
            IsTransferDisabled = false,
            Supply = 0
        };

        /// <summary>
        /// Not Burnable & Transferable
        /// </summary>
        private TokenInfo BobCoinTokenInfo { get; set; } = new TokenInfo
        {
            Symbol = "BOB",
            TokenName = "For testing multi-token contract",
            TotalSupply = 1_000_000_000_0000,
            Decimals = 4,
            IsBurnable = false,
            Issuer = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[0].PublicKey),
            IsTransferDisabled = false,
            Supply = 0
        };

        /// <summary>
        /// Not Burnable & Not Transferable
        /// </summary>
        private TokenInfo EanCoinTokenInfo { get; set; } = new TokenInfo
        {
            Symbol = "EAN",
            TokenName = "For testing multi-token contract",
            TotalSupply = 1_000_000_000,
            Decimals = 0,
            IsBurnable = true,
            Issuer = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[0].PublicKey),
            IsTransferDisabled = true,
            Supply = 0
        };

        public MultiTokenContractTests()
        {
            var category = KernelConstants.CodeCoverageRunnerCategory;

            // TokenContract
            {
                var code = TokenContractCode;
                TokenContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(category, code,
                    TokenSmartContractAddressNameProvider.Name, DefaultKeyPair));
                TokenContractStub =
                    GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultKeyPair);
            }
            
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

            //TokenConvertContract
            {
                var code = TokenConverterContractCode;
                TokenConverterContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(category, code,
                    TokenConverterSmartContractAddressNameProvider.Name, DefaultKeyPair));
                TokenConverterContractStub =
                    GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterContractAddress,
                        DefaultKeyPair);
            }
            
            //BasicFunctionContract
            {
                BasicFunctionContractAddress = AsyncHelper.RunSync(()=> DeploySystemSmartContract(
                    category, BasicFunctionContractCode,
                    BasicFunctionContractName, DefaultKeyPair));
                BasicFunctionContractStub =
                    GetTester<BasicFunctionContractContainer.BasicFunctionContractStub>(BasicFunctionContractAddress,
                        DefaultKeyPair);

                OtherBasicFunctionContractAddress = AsyncHelper.RunSync(()=> DeploySystemSmartContract(
                    category, BasicFunctionContractCode,
                    OtherBasicFunctionContractName, DefaultKeyPair));
                OtherBasicFunctionContractStub =
                    GetTester<BasicFunctionContractContainer.BasicFunctionContractStub>(
                        OtherBasicFunctionContractAddress,
                        DefaultKeyPair);
            }
            
        }

        [Fact(DisplayName = "[MultiToken] Create token test.")]
        public async Task MultiTokenContract_Create()
        {
            // Check token information before creating.
            {
                var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {
                    Symbol = AliceCoinTokenInfo.Symbol
                });
                tokenInfo.ShouldBe(new TokenInfo());
            }

            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                TokenName = AliceCoinTokenInfo.TokenName,
                TotalSupply = AliceCoinTokenInfo.TotalSupply,
                Decimals = AliceCoinTokenInfo.Decimals,
                Issuer = AliceCoinTokenInfo.Issuer,
                IsBurnable = AliceCoinTokenInfo.IsBurnable,
                IsTransferDisabled = AliceCoinTokenInfo.IsTransferDisabled
            });

            // Check token information after creating.
            {
                var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {
                    Symbol = AliceCoinTokenInfo.Symbol
                });
                tokenInfo.ShouldBe(AliceCoinTokenInfo);
            }
        }

        [Fact(DisplayName = "[MultiToken] Create different tokens.")]
        public async Task MultiTokenContract_Create_NotSame()
        {
            await MultiTokenContract_Create();

            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = BobCoinTokenInfo.Symbol,
                TokenName = BobCoinTokenInfo.TokenName,
                TotalSupply = BobCoinTokenInfo.TotalSupply,
                Decimals = BobCoinTokenInfo.Decimals,
                Issuer = BobCoinTokenInfo.Issuer,
                IsBurnable = BobCoinTokenInfo.IsBurnable,
                IsTransferDisabled = BobCoinTokenInfo.IsTransferDisabled
            });

            // Check token information after creating.
            {
                var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {
                    Symbol = BobCoinTokenInfo.Symbol
                });
                tokenInfo.ShouldNotBe(AliceCoinTokenInfo);
            }
        }
        
        [Fact(DisplayName = "[MultiToken] Create Token use custom address")]
        public async Task MultiTokenContract_Create_UseCustomAddress()
        {
            var transactionResult = (await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Decimals = 2,
                IsBurnable = true,
                Issuer = DefaultAddress,
                TokenName = "elf test token",
                TotalSupply = AliceCoinTotalAmount,
                LockWhiteList =
                {
                    User1Address
                }
            })).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Addresses in lock white list should be system contract addresses");
        }

        [Fact(DisplayName = "[MultiToken] Issue token test")]
        public async Task MultiTokenContract_Issue()
        {
            MultiTokenContract_Create();
            //issue AliceToken amount of 1000_00L to DefaultAddress 
            {
                var result = await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = AliceCoinTokenInfo.Symbol,
                    Amount = AliceCoinTotalAmount,
                    To = DefaultAddress,
                    Memo = "first issue token."
                });
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = DefaultAddress,
                    Symbol = AliceCoinTokenInfo.Symbol
                })).Balance;
                balance.ShouldBe(AliceCoinTotalAmount);
            }
            //issue AliceToken amount of 1000L to User1Address 
            {
                var result = await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = AliceCoinTokenInfo.Symbol,
                    Amount = 1000,
                    To = User1Address,
                    Memo = "first issue token."
                });
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = User1Address,
                    Symbol = AliceCoinTokenInfo.Symbol
                })).Balance;
                balance.ShouldBe(1000);
            }
            //Issue AliceToken amount of 1000L to User2Address  
            {
                var result = await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = AliceCoinTokenInfo.Symbol,
                    Amount = 1000,
                    To = User2Address,
                    Memo = "second issue token."
                });
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = User2Address,
                    Symbol = AliceCoinTokenInfo.Symbol
                })).Balance;
                balance.ShouldBe(1000);
            }
        }

        [Fact(DisplayName = "[MultiToken] Issue out of total amount")]
        public async Task MultiTokenContract_Issue_OutOfAmount()
        {
            MultiTokenContract_Create();
            //issue AliceToken amount of 1000L to User1Address 
            var result = (await TokenContractStub.Issue.SendAsync(new IssueInput()
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Amount = AliceCoinTokenInfo.TotalSupply+1,
                To = User1Address,
                Memo = "first issue token."
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains($"Total supply exceeded").ShouldBeTrue();
        }
        
        
    }
}