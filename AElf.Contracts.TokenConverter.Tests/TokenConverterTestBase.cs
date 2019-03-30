using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Contracts.TokenConverter
{
    public class TokenConverterTestBase:ContractTestBase<TokenConverterTestModule>
    {
        protected ISmartContractAddressService ContractAddressService =>
            Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();

        protected Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
        
        protected Address BasicZeroContractAddress;
        
        protected Address TokenContractAddress;
        
        protected Address TokenConverterContractAddress;
        
        internal BasicContractZeroContainer.BasicContractZeroTester ContractZeroTester =>
            GetTester<BasicContractZeroContainer.BasicContractZeroTester>(ContractZeroAddress, DefaultSenderKeyPair);

        internal TokenContractContainer.TokenContractTester TokenContractTester;
        
        internal TokenConverterContractContainer.TokenConverterContractTester DefaultTester;
        
        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected ECKeyPair FeeReceiverKeyPair { get; } = SampleECKeyPairs.KeyPairs[10];
        protected Address FeeReceiverAddress => Address.FromPublicKey(ManagerKeyPair.PublicKey);
        protected ECKeyPair ManagerKeyPair { get; } = SampleECKeyPairs.KeyPairs[11];
        protected Address ManagerAddress => Address.FromPublicKey(ManagerKeyPair.PublicKey);
        protected ECKeyPair FoundationKeyPair { get; } = SampleECKeyPairs.KeyPairs[12];
        
        protected async Task DeployContractsAsync()
        {
            {
                // TokenContract
                var result = await ContractZeroTester.DeploySmartContract.SendAsync(new ContractDeploymentInput()
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location))
                });
                TokenContractAddress = result.Output;
                TokenContractTester =
                    GetTester<TokenContractContainer.TokenContractTester>(TokenContractAddress, DefaultSenderKeyPair);

                await TokenContractTester.Create.SendAsync(new CreateInput()
                {
                    Symbol = "ELF",
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "elf token",
                    TotalSupply = 1000_0000L,
                    Issuer = DefaultSender
                });
                await TokenContractTester.Issue.SendAsync(new IssueInput()
                {
                    Symbol = "ELF",
                    Amount = 1000_000L,
                    To = DefaultSender,
                    Memo = "Set for token converter."
                });
            }
            {
                // TokenConverterContract
                var result = await ContractZeroTester.DeploySmartContract.SendAsync(new ContractDeploymentInput()
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenConverterContract).Assembly.Location))
                });
                TokenConverterContractAddress = result.Output;
                DefaultTester = GetTester<TokenConverterContractContainer.TokenConverterContractTester>(
                    TokenConverterContractAddress, DefaultSenderKeyPair);
            }
        }

        protected async Task<long> GetBalanceAsync(string symbol, Address owner)
        {
            var balanceResult = await TokenContractTester.GetBalance.CallAsync(
                new GetBalanceInput()
                {
                    Owner = owner,
                    Symbol = symbol
                });
            return balanceResult.Balance;
        }
    }
}