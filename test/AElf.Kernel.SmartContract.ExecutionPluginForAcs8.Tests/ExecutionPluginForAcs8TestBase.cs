using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests
{
    public class ExecutionPluginForAcs8TestBase : ContractTestBase<ExecutionPluginForAcs8TestModule>
    {
        internal const long CpuUnitPrice = 1_00000000;
        internal const long NetUnitPrice = 1_00000000;
        internal const long StoUnitPrice = 1_00000000;

        //init connectors
        internal Connector ElfConnector = new Connector
        {
            Symbol = "ELF",
            VirtualBalance = 100_000_00000000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true
        };

        internal Connector CpuConnector = new Connector
        {
            Symbol = "CPU",
            VirtualBalance = 100_000_00000000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true// For testing
        };
        
        internal Connector StoConnector = new Connector
        {
            Symbol = "STO",
            VirtualBalance = 100_000_00000000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true// For testing
        };
        
        internal Connector NetConnector = new Connector
        {
            Symbol = "NET",
            VirtualBalance = 100_000_00000000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true// For testing
        };

        internal Address TestContractAddress { get; set; }
        internal Address TokenContractAddress { get; set; }
        internal Address TokenConverterAddress { get; set; }
        internal Address TreasuryContractAddress { get; set; }
        internal TestContract.ContractContainer.ContractStub DefaultTester { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        internal TokenConverterContractContainer.TokenConverterContractStub TokenConverterContractStub { get; set; }
        internal TreasuryContractContainer.TreasuryContractStub TreasuryContractStub { get; set; }

        internal ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        internal ECKeyPair OtherTester => SampleECKeyPairs.KeyPairs[1];
        internal Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected ECKeyPair FeeReceiverKeyPair { get; } = SampleECKeyPairs.KeyPairs[10];
        protected Address FeeReceiverAddress => Address.FromPublicKey(FeeReceiverKeyPair.PublicKey);
        protected ECKeyPair ManagerKeyPair { get; } = SampleECKeyPairs.KeyPairs[11];
        protected Address ManagerAddress => Address.FromPublicKey(ManagerKeyPair.PublicKey);

        protected async Task InitializeContracts()
        {
            await DeployContractsAsync();
            await InitializeTokenAsync();
            await InitializeTokenConverterAsync();
            await InitializeTreasuryContractAsync();
        }
        
        private async Task DeployContractsAsync()
        {
            const int category = KernelConstants.CodeCoverageRunnerCategory;
            // Profit contract
            {
                var code = Codes.Single(kv => kv.Key.Contains("Profit")).Value;
                await DeploySystemSmartContract(category, code,
                    ProfitSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
            }

            // Token contract
            {
                var code = Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
                TokenContractAddress = await DeploySystemSmartContract(category, code,
                    TokenSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
                TokenContractStub =
                    GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultSenderKeyPair);
            }

            // Token converter
            {
                var code = Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value;
                TokenConverterAddress = await DeploySystemSmartContract(category, code,
                    TokenConverterSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
                TokenConverterContractStub =
                    GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterAddress,
                        DefaultSenderKeyPair);
            }
            
            // Token converter
            {
                var code = Codes.Single(kv => kv.Key.Contains("Treasury")).Value;
                TreasuryContractAddress = await DeploySystemSmartContract(category, code,
                    TreasurySmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
                TreasuryContractStub =
                    GetTester<TreasuryContractContainer.TreasuryContractStub>(TreasuryContractAddress,
                        DefaultSenderKeyPair);
            }

            // Test contract
            {
                var code = Codes.Single(kv => kv.Key.Contains("TestContract")).Value;
                TestContractAddress = await DeployContractAsync(category, code, Hash.FromString("TestContract"),
                    DefaultSenderKeyPair);
                DefaultTester =
                    GetTester<TestContract.ContractContainer.ContractStub>(TestContractAddress, DefaultSenderKeyPair);
            }
        }

        private async Task InitializeTokenAsync()
        {
            const long totalSupply = 1_000_000_000_00000000;
            const long issueAmount = 1_000_000_00000000;
            const long issueAmountToConverter = 100_000_000_00000000;
            //init elf token
            {
                var createResult = await TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = "ELF",
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "elf token",
                    TotalSupply = totalSupply,
                    Issuer = DefaultSender,
                    LockWhiteList = {TreasuryContractAddress, TokenConverterAddress}
                });

                createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                {
                    var issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput()
                    {
                        Symbol = "ELF",
                        Amount = issueAmount,
                        To = DefaultSender,
                    });
                    issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                }
                {
                    var issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput()
                    {
                        Symbol = "ELF",
                        Amount = issueAmountToConverter,
                        To = TokenConverterAddress,
                        Memo = "Set for elf token converter."
                    });
                    issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                }
            }
            
            //init resource token - CPU
            {
                var createResult = await TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = "CPU",
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "cpu token",
                    TotalSupply = totalSupply,
                    Issuer = DefaultSender,
                    LockWhiteList = {TreasuryContractAddress, TokenConverterAddress}
                });

                createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = "CPU",
                    Amount = issueAmount,
                    To = DefaultSender,
                    Memo = "Set for cpu token converter."
                });
                issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
            
            //init resource token - STO
            {
                var createResult = await TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = "STO",
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "sto token",
                    TotalSupply = totalSupply,
                    Issuer = DefaultSender,
                    LockWhiteList = {TreasuryContractAddress, TokenConverterAddress}
                });

                createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = "STO",
                    Amount = issueAmount,
                    To = DefaultSender,
                    Memo = "Set for sto token converter."
                });
                issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
            
            //init resource token - NET
            {
                var createResult = await TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = "NET",
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "net token",
                    TotalSupply = totalSupply,
                    Issuer = DefaultSender,
                    LockWhiteList = {TreasuryContractAddress, TokenConverterAddress}
                });

                createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = "NET",
                    Amount = issueAmount,
                    To = DefaultSender,
                    Memo = "Set for net token converter."
                });
                issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            await TokenContractStub.SetResourceTokenUnitPrice.SendAsync(new SetResourceTokenUnitPriceInput
            {
                CpuUnitPrice = CpuUnitPrice,
                NetUnitPrice = NetUnitPrice,
                StoUnitPrice = StoUnitPrice
            });
        }

        private async Task InitializeTokenConverterAsync()
        {
            var input = new InitializeInput
            {
                BaseTokenSymbol = "ELF",
                FeeRate = "0.005",
                ManagerAddress = ManagerAddress,
                TokenContractAddress = TokenContractAddress,
                FeeReceiverAddress = FeeReceiverAddress,
                Connectors = { ElfConnector, CpuConnector, StoConnector, NetConnector }
            };

            var initializeResult = await TokenConverterContractStub.Initialize.SendAsync(input);
            initializeResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task InitializeTreasuryContractAsync()
        {
            await TreasuryContractStub.InitialTreasuryContract.SendAsync(new Empty());
            await TreasuryContractStub.InitialMiningRewardProfitItem.SendAsync(new Empty());
        }
    }
}