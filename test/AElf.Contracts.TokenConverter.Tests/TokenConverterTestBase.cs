using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Types;

namespace AElf.Contracts.TokenConverter
{
    public class TokenConverterTestBase:ContractTestBase<TokenConverterTestModule>
    {
        protected Address TokenContractAddress;
        
        protected Address TokenConverterContractAddress;

        internal TokenContractContainer.TokenContractStub TokenContractStub;
        
        internal TokenConverterContractContainer.TokenConverterContractStub DefaultStub;
        
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
                var category = KernelConstants.CodeCoverageRunnerCategory;
                var code = Codes.Single(kv => kv.Key.Split(",").First().EndsWith("MultiToken")).Value;
                TokenContractAddress = await DeployContractAsync(category, code, Hash.FromString("MultiToken"), DefaultSenderKeyPair);
                TokenContractStub =
                    GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultSenderKeyPair);

                await TokenContractStub.Create.SendAsync(new CreateInput()
                {
                    Symbol = "ELF",
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "elf token",
                    TotalSupply = 1000_0000L,
                    Issuer = DefaultSender
                });
                await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = "ELF",
                    Amount = 1000_000L,
                    To = DefaultSender,
                    Memo = "Set for token converter."
                });
            }
            {
                // TokenConverterContract
                var category = KernelConstants.CodeCoverageRunnerCategory;
                var code = Codes.Single(kv => kv.Key.Split(",").First().EndsWith("TokenConverter")).Value;
                TokenConverterContractAddress = await DeployContractAsync(category, code, Hash.FromString("TokenConverter"), DefaultSenderKeyPair);
                DefaultStub = GetTester<TokenConverterContractContainer.TokenConverterContractStub>(
                    TokenConverterContractAddress, DefaultSenderKeyPair);
            }
        }

        protected async Task<long> GetBalanceAsync(string symbol, Address owner)
        {
            var balanceResult = await TokenContractStub.GetBalance.CallAsync(
                new GetBalanceInput()
                {
                    Owner = owner,
                    Symbol = symbol
                });
            return balanceResult.Balance;
        }
    }
}