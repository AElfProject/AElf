using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.Types;

namespace AElf.Contracts.TokenConverter
{
    public class TokenConverterTestBase:ContractTestBase<TokenConverterTestModule>
    {
        protected Address TokenContractAddress;
        
        protected Address TokenConverterContractAddress;

        internal TokenContractContainer.TokenContractStub TokenContractStub;
        internal TokenContractContainer.TokenContractStub AuthorizedTokenContractStub;
        
        internal TokenConverterContractContainer.TokenConverterContractStub DefaultStub;
        internal TokenConverterContractContainer.TokenConverterContractStub AuthorizedTokenConvertStub;
        
        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected Address FeeReceiverAddress => Address.FromPublicKey(ManagerKeyPair.PublicKey);
        protected ECKeyPair ManagerKeyPair { get; } = SampleECKeyPairs.KeyPairs[11];
        protected Address ManagerAddress => Address.FromPublicKey(ManagerKeyPair.PublicKey);
        
        protected async Task DeployContractsAsync()
        {
            {
                // TokenContract
                var category = KernelConstants.CodeCoverageRunnerCategory;
                var code = Codes.Single(kv => kv.Key.Split(",").First().EndsWith("MultiToken")).Value;
                TokenContractAddress = await DeploySystemSmartContract(category, code, TokenSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
                TokenContractStub =
                    GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultSenderKeyPair);
                AuthorizedTokenContractStub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, ManagerKeyPair);
            }
            {
                // TokenConverterContract
                var category = KernelConstants.CodeCoverageRunnerCategory;
                var code = Codes.Single(kv => kv.Key.Split(",").First().EndsWith("TokenConverter")).Value;
                TokenConverterContractAddress = await DeploySystemSmartContract(category, code, TokenConverterSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
                DefaultStub = GetTester<TokenConverterContractContainer.TokenConverterContractStub>(
                    TokenConverterContractAddress, DefaultSenderKeyPair);
                AuthorizedTokenConvertStub = GetTester<TokenConverterContractContainer.TokenConverterContractStub>(
                    TokenConverterContractAddress, ManagerKeyPair);
            }
            
            await TokenContractStub.Create.SendAsync(new CreateInput()
            {
                Symbol = "ELF",
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = 1000_0000_0000L,
                Issuer = DefaultSender,
                LockWhiteList = { TokenConverterContractAddress} 
            });
            await TokenContractStub.Issue.SendAsync(new IssueInput()
            {
                Symbol = "ELF",
                Amount = 1000_000L,
                To = DefaultSender,
                Memo = "Set for token converter."
            });
            await TokenContractStub.Issue.SendAsync(new IssueInput()
            {
                Symbol = "ELF",
                Amount = 100_0000_0000L,
                To = ManagerAddress,
                Memo = "Set for token converter."
            });
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