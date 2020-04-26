using System.Threading.Tasks;
using Acs2;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.Contracts.Referendum;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.TestKit;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

// ReSharper disable CheckNamespace
namespace AElf.Contracts.MultiToken
{
    public partial class MultiTokenContractTests : MultiTokenContractTestBase
    {
        private const long TotalSupply = 1000_000_000_00000000;
        private readonly int _chainId;

        private TokenInfo NativeTokenInfo => new TokenInfo
        {
            Symbol = GetRequiredService<IOptionsSnapshot<HostSmartContractBridgeContextOptions>>().Value
                .ContextVariables[ContextVariableDictionary.NativeSymbolName],
            TokenName = "Native token",
            TotalSupply = TotalSupply,
            Decimals = 8,
            IsBurnable = true,
            Issuer = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[0].PublicKey),
            Supply = 0,
            IssueChainId = _chainId
        };
        
        private TokenInfo PrimaryTokenInfo => new TokenInfo
        {
            Symbol = "PRIMARY",
            TokenName = "Primary token",
            TotalSupply = TotalSupply,
            Decimals = 8,
            IsBurnable = true,
            Issuer = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[0].PublicKey),
            Supply = 0,
            IssueChainId = _chainId
        };

        /// <summary>
        /// Burnable & Transferable
        /// </summary>
        private TokenInfo AliceCoinTokenInfo => new TokenInfo
        {
            Symbol = "ALICE",
            TokenName = "For testing multi-token contract",
            TotalSupply = TotalSupply,
            Decimals = 8,
            IsBurnable = true,
            Issuer = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[0].PublicKey),
            Supply = 0,
            IssueChainId = _chainId
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
            Supply = 0
        };

        private Connector RamConnector = new Connector
        {
            Symbol = "ALICE",
            VirtualBalance = 0,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = false,
            RelatedSymbol = "ELF"
        };

        private Connector BaseConnector = new Connector
        {
            Symbol = "ELF",
            VirtualBalance = 0,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = false
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
                    GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, DefaultKeyPair);
                Acs2BaseStub = GetTester<ACS2BaseContainer.ACS2BaseStub>(TokenContractAddress, DefaultKeyPair);
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

            //ReferendumContract
            {
                var code = ReferendumContractCode;
                ReferendumContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(category, code,
                    ReferendumSmartContractAddressNameProvider.Name, DefaultKeyPair));
                ReferendumContractStub =
                    GetTester<ReferendumContractContainer.ReferendumContractStub>(ReferendumContractAddress,
                        DefaultKeyPair);
            }

            //BasicFunctionContract
            {
                BasicFunctionContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(
                    category, BasicFunctionContractCode,
                    BasicFunctionContractName, DefaultKeyPair));
                BasicFunctionContractStub =
                    GetTester<BasicFunctionContractContainer.BasicFunctionContractStub>(BasicFunctionContractAddress,
                        DefaultKeyPair);

                OtherBasicFunctionContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(
                    category, OtherBasicFunctionContractCode,
                    OtherBasicFunctionContractName, DefaultKeyPair));
                OtherBasicFunctionContractStub =
                    GetTester<BasicFunctionContractContainer.BasicFunctionContractStub>(
                        OtherBasicFunctionContractAddress,
                        DefaultKeyPair);
            }
            _chainId = GetRequiredService<IOptionsSnapshot<ChainOptions>>().Value.ChainId;
            
            //ParliamentContract
            {
                var code = ParliamentCode;
                ParliamentContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(category, code,
                    ParliamentSmartContractAddressNameProvider.Name, DefaultKeyPair));
                ParliamentContractStub =
                    GetTester<ParliamentContractContainer.ParliamentContractStub>(ParliamentContractAddress,
                        DefaultKeyPair);
                AsyncHelper.RunSync(InitializeParliamentContract);
            }
            
            //AEDPOSContract
            {
                ConsensusContractAddress = AsyncHelper.RunSync(() =>
                    DeploySystemSmartContract(
                        KernelConstants.CodeCoverageRunnerCategory,
                        ConsensusContractCode,
                        HashHelper.ComputeFromString("AElf.ContractNames.Consensus"),
                        DefaultKeyPair
                    ));
                AEDPoSContractStub = GetConsensusContractTester(DefaultKeyPair);
                AsyncHelper.RunSync(async () => await InitializeAElfConsensus());
            }
            
            //AssociationContract
            {
                var code = AssociationContractCode;
                AsyncHelper.RunSync(() => DeploySystemSmartContract(category, code,
                    AssociationSmartContractAddressNameProvider.Name, DefaultKeyPair));
            }
        }

        private async Task CreateNativeTokenAsync()
        {
            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = NativeTokenInfo.Symbol,
                TokenName = NativeTokenInfo.TokenName,
                TotalSupply = NativeTokenInfo.TotalSupply,
                Decimals = NativeTokenInfo.Decimals,
                Issuer = NativeTokenInfo.Issuer,
                IsBurnable = NativeTokenInfo.IsBurnable,
                LockWhiteList =
                {
                    BasicFunctionContractAddress,
                    OtherBasicFunctionContractAddress,
                    TokenConverterContractAddress,
                    TreasuryContractAddress
                }
            });
        }
        
        private async Task CreatePrimaryTokenAsync()
        {
            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = NativeTokenInfo.Symbol,
                TokenName = NativeTokenInfo.TokenName,
                TotalSupply = NativeTokenInfo.TotalSupply,
                Decimals = NativeTokenInfo.Decimals,
                Issuer = NativeTokenInfo.Issuer,
                IsBurnable = NativeTokenInfo.IsBurnable
            });


            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Decimals = PrimaryTokenInfo.Decimals,
                IsBurnable = PrimaryTokenInfo.IsBurnable,
                Issuer = PrimaryTokenInfo.Issuer,
                TotalSupply = PrimaryTokenInfo.TotalSupply,
                Symbol = PrimaryTokenInfo.Symbol,
                TokenName = PrimaryTokenInfo.TokenName,
                IssueChainId = PrimaryTokenInfo.IssueChainId
            });

            await TokenContractStub.SetPrimaryTokenSymbol.SendAsync(
                new SetPrimaryTokenSymbolInput
                {
                    Symbol = PrimaryTokenInfo.Symbol
                });
        }

        private async Task CreateNormalTokenAsync()
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
                LockWhiteList =
                {
                    BasicFunctionContractAddress,
                    OtherBasicFunctionContractAddress,
                    TokenConverterContractAddress,
                    TreasuryContractAddress
                }
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

        private async Task TokenConverter_Converter()
        {
            await TreasuryContractStub.InitialTreasuryContract.SendAsync(new Empty());

            await TreasuryContractStub.InitialMiningRewardProfitItem.SendAsync(new Empty());

            await TokenConverterContractStub.Initialize.SendAsync(new TokenConverter.InitializeInput
            {
                Connectors = {RamConnector, BaseConnector},
                BaseTokenSymbol = "ELF",
                FeeRate = "0.2",
            });
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = "ELF",
                Amount = 1000L,
                Memo = "ddd",
                To = TokenConverterContractAddress
            });
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = "ELF",
                Amount = 1000L,
                Memo = "ddd",
                To = TokenConverterContractAddress
            });
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Amount = 1000L,
                Memo = "ddd",
                To = TokenConverterContractAddress
            });
        }

        [Fact(DisplayName = "[MultiToken] Create different tokens.")]
        public async Task MultiTokenContract_Create_NotSame_Test()
        {
            await CreateAndIssueMultiTokensAsync();

            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = BobCoinTokenInfo.Symbol,
                TokenName = BobCoinTokenInfo.TokenName,
                TotalSupply = BobCoinTokenInfo.TotalSupply,
                Decimals = BobCoinTokenInfo.Decimals,
                Issuer = BobCoinTokenInfo.Issuer,
                IsBurnable = BobCoinTokenInfo.IsBurnable,
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
        public async Task MultiTokenContract_Create_UseCustomAddress_Test()
        {
            var transactionResult = (await TokenContractStub.Create.SendWithExceptionAsync(new CreateInput
            {
                Symbol = NativeTokenInfo.Symbol,
                Decimals = 2,
                IsBurnable = true,
                Issuer = DefaultAddress,
                TokenName = NativeTokenInfo.TokenName,
                TotalSupply = AliceCoinTotalAmount,
                LockWhiteList =
                {
                    User1Address
                }
            })).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Addresses in lock white list should be system contract addresses");
        }

        private async Task CreateAndIssueMultiTokensAsync()
        {
            await CreateNativeTokenAsync();
            await CreateNormalTokenAsync();
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

            //issue ELF amount of 1000_00L to DefaultAddress 
            {
                var result = await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = "ELF",
                    Amount = AliceCoinTotalAmount,
                    To = DefaultAddress,
                    Memo = "first issue token."
                });
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = DefaultAddress,
                    Symbol = "ELF"
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
        public async Task MultiTokenContract_Issue_OutOfAmount_Test()
        {
            await CreateAndIssueMultiTokensAsync();
            //issue AliceToken amount of 1000L to User1Address 
            var result = (await TokenContractStub.Issue.SendWithExceptionAsync(new IssueInput()
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Amount = AliceCoinTokenInfo.TotalSupply + 1,
                To = User1Address,
                Memo = "first issue token."
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains($"Total supply exceeded").ShouldBeTrue();
        }

        [Fact]
        public async Task AddNativeTokenWhiteList_Test()
        {
            await CreateNativeTokenAsync();
            {
                var tx = await TokenContractStub.AddTokenWhiteList.SendWithExceptionAsync(new AddTokeWhiteListInput
                {
                    TokenSymbol = NativeTokenInfo.Symbol,
                    Address = TokenContractAddress
                });
                tx.TransactionResult.Error.ShouldContain("No permission.");
            }
            {
                var tx = await TokenContractStub.AddTokenWhiteList.SendWithExceptionAsync(new AddTokeWhiteListInput
                {
                    TokenSymbol = NativeTokenInfo.Symbol,
                    Address = TokenContractAddress
                });
                tx.TransactionResult.Error.ShouldContain("No permission.");
            }
        }
        
        [Fact]
        public async Task AddChainPrimaryTokenWhiteList_Test()
        {
            await CreatePrimaryTokenAsync();
            {
                var tx = await TokenContractStub.AddTokenWhiteList.SendWithExceptionAsync(new AddTokeWhiteListInput
                {
                    TokenSymbol = PrimaryTokenInfo.Symbol,
                    Address = TokenContractAddress
                });
                tx.TransactionResult.Error.ShouldContain("No permission.");
            }
            {
                var tx = await TokenContractStub.AddTokenWhiteList.SendWithExceptionAsync(new AddTokeWhiteListInput
                {
                    TokenSymbol = PrimaryTokenInfo.Symbol,
                    Address = TokenContractAddress
                });
                tx.TransactionResult.Error.ShouldContain("No permission.");
            }
        }

        [Fact]
        public async Task AddNormalTokenWhiteList_Test()
        {
            await CreateAndIssueMultiTokensAsync();
            {
                var tx = await TokenContractStub.AddTokenWhiteList.SendWithExceptionAsync(new AddTokeWhiteListInput
                {
                    TokenSymbol = AliceCoinTokenInfo.Symbol
                });
                tx.TransactionResult.Error.ShouldContain("Invalid input.");
            }
            {
                var tx = await TokenContractStub.AddTokenWhiteList.SendWithExceptionAsync(new AddTokeWhiteListInput
                {
                    TokenSymbol = AliceCoinTokenInfo.Symbol,
                    Address = TokenContractAddress
                });
                tx.TransactionResult.Error.ShouldContain("No permission.");
            }
        }

        [Fact]
        public async Task IssueTokenWithDifferentMemoLength_Test()
        {
            await CreateNativeTokenAsync();
            await CreateNormalTokenAsync();
            {
                var result = await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = AliceCoinTokenInfo.Symbol,
                    Amount = AliceCoinTotalAmount,
                    To = DefaultAddress,
                    Memo = "MemoTest MemoTest MemoTest MemoTest MemoTest MemoTest MemoTest.."
                });
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
            {
                var result = await TokenContractStub.Issue.SendWithExceptionAsync(new IssueInput()
                {
                    Symbol = AliceCoinTokenInfo.Symbol,
                    Amount = AliceCoinTotalAmount,
                    To = DefaultAddress,
                    Memo = "MemoTest MemoTest MemoTest MemoTest MemoTest MemoTest MemoTest..."
                });
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                result.TransactionResult.Error.ShouldContain("Invalid memo size.");
            }
        }
    }
}