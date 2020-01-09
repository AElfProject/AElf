using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Contracts.Association
{
    public class AssociationContractTestBase<T> : ContractTestBase<T> where T : ContractTestModule
    {
        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected ECKeyPair Reviewer1KeyPair => SampleECKeyPairs.KeyPairs[1];
        protected ECKeyPair Reviewer2KeyPair => SampleECKeyPairs.KeyPairs[2];
        protected ECKeyPair Reviewer3KeyPair => SampleECKeyPairs.KeyPairs[3];
        protected Address Reviewer1 => Address.FromPublicKey(Reviewer1KeyPair.PublicKey);
        protected Address Reviewer2 => Address.FromPublicKey(Reviewer2KeyPair.PublicKey);
        protected Address Reviewer3 => Address.FromPublicKey(Reviewer3KeyPair.PublicKey);

        protected IBlockTimeProvider BlockTimeProvider =>
            Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();

        protected new Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
        protected Address TokenContractAddress { get; set; }
        protected Address AssociationContractAddress { get; set; }

        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        internal AssociationContractContainer.AssociationContractStub AssociationContractStub { get; set; }

        internal AssociationContractContainer.AssociationContractStub AnotherChainAssociationContractStub { get; set; }
        
        private byte[] AssociationContractCode => Codes.Single(kv => kv.Key.Contains("Association")).Value;
        private byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;

        protected void DeployContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);

            //deploy Association contract
            AssociationContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    AssociationContractCode,
                    ParliamentSmartContractAddressNameProvider.Name,
                    DefaultSenderKeyPair
                ));

            AssociationContractStub = GetAssociationContractTester(DefaultSenderKeyPair);
            TokenContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    TokenContractCode,
                    TokenSmartContractAddressNameProvider.Name,
                    DefaultSenderKeyPair));
            TokenContractStub = GetTokenContractTester(DefaultSenderKeyPair);
            AsyncHelper.RunSync(async () => await InitializeTokenAsync());
        }
        
        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }

        internal AssociationContractContainer.AssociationContractStub GetAssociationContractTester(ECKeyPair keyPair)
        {
            return GetTester<AssociationContractContainer.AssociationContractStub>(AssociationContractAddress, keyPair);
        }

        private async Task InitializeTokenAsync()
        {
            const string symbol = "ELF";
            const long totalSupply = 100_000_000;
            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = symbol,
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = totalSupply,
                Issuer = DefaultSender,
            });
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = symbol,
                Amount = totalSupply - 20 * 100_000L,
                To = DefaultSender,
                Memo = "Issue token to default user.",
            });
        }
    }
}