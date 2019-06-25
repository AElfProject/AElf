using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.TestKit;
using AElf.Contracts.TokenConverter;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs5.Tests
{
    public class ExecutionPluginForAcs5TestBase : ContractTestBase<ExecutionPluginForAcs5TestModule>
    {
        //init connector
        internal Connector ELFConnector = new Connector
        {
            Symbol = "ELF",
            VirtualBalance = 100_0000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true
        };

        internal Connector RamConnector = new Connector
        {
            Symbol = "RAM",
            VirtualBalance = 0,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = false
        };
        
        internal Address TestContractAddress { get; set; }
        internal Address TokenContractAddress { get; set; }
        internal Address TokenConverterAddress { get; set; }
        internal Address ProfitContractAddress { get; set; }
        
        internal TestContract.ContractContainer.ContractStub DefaultTester { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        internal TokenConverterContractContainer.TokenConverterContractStub TokenConverterContractStub { get; set; }
        internal ProfitContractContainer.ProfitContractStub ProfitContractStub { get; set; }
        
        internal ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
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
            await InitializeProfitAsync();
        }
        
        private async Task DeployContractsAsync()
        {
            const int category = KernelConstants.CodeCoverageRunnerCategory;
            //Token contract
            {
                var code = Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
                TokenContractAddress = await DeploySystemSmartContract(category, code,
                    TokenSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
                TokenContractStub =
                    GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultSenderKeyPair);
            }
            
            //Token converter
            {
                var code = Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value;
                TokenConverterAddress = await DeploySystemSmartContract(category, code,
                    TokenConverterSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
                TokenConverterContractStub =
                    GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterAddress, DefaultSenderKeyPair);
            }
            
            //Profit contract
            {
                var code = Codes.Single(kv => kv.Key.Contains("Profit")).Value;
                ProfitContractAddress = await DeploySystemSmartContract(category, code,
                    ProfitSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
                ProfitContractStub =
                    GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, DefaultSenderKeyPair);
            }
            //Test contract
            {
                var code = Codes.Single(kv => kv.Key.Contains("TestContract")).Value;
                TestContractAddress = await DeployContractAsync(category, code, DefaultSenderKeyPair);
                DefaultTester =
                    GetTester<TestContract.ContractContainer.ContractStub>(TestContractAddress, DefaultSenderKeyPair);
            }
        }
        private async Task InitializeTokenAsync()
        {
            var createResult = await TokenContractStub.CreateNativeToken.SendAsync(new CreateNativeTokenInput()
            {
                Symbol = "ELF",
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = 1000_0000L,
                Issuer = DefaultSender
            });

            createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput()
            {
                Symbol = "ELF",
                Amount = 1000_000L,
                To = DefaultSender,
                Memo = "Set for token converter."
            });
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
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
                Connectors = { ELFConnector, RamConnector }
            };

            var initializeResult = await TokenConverterContractStub.Initialize.SendAsync(input);
            initializeResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        
        private async Task InitializeProfitAsync()
        {
            var initializeResult = await ProfitContractStub.InitializeProfitContract.SendAsync(new Empty());
            initializeResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}