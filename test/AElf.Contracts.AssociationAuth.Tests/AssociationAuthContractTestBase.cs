using System.IO;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Contracts.AssociationAuth
{
    public class AssociationAuthContractTestBase : ContractTestBase<AssociationAuthContractTestAElfModule>
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
        protected Address AssociationAuthContractAddress { get; set; }
        
        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        internal AssociationAuthContractContainer.AssociationAuthContractStub AssociationAuthContractStub { get; set; }

        protected void DeployContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);

            //deploy AssociationAuth contract
            {
                var code = File.ReadAllBytes(typeof(AssociationAuthContract).Assembly.Location);
                AssociationAuthContractAddress = AsyncHelper.RunSync(() =>
                    BasicContractZeroStub.DeploySmartContract.SendAsync(
                        new ContractDeploymentInput()
                        {
                            Category = KernelConstants.CodeCoverageRunnerCategory,
                            Code = ByteString.CopyFrom(code)
                        })).Output;
                AsyncHelper.RunSync(() =>
                    SetContractCacheAsync(AssociationAuthContractAddress, Hash.FromRawBytes(code)));
                AssociationAuthContractStub = GetAssociationAuthContractTester(DefaultSenderKeyPair);
            }

            {
                var code = File.ReadAllBytes(typeof(TokenContract).Assembly.Location);
                TokenContractAddress = AsyncHelper.RunSync(() =>
                    BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                        new SystemContractDeploymentInput
                        {
                            Category = KernelConstants.CodeCoverageRunnerCategory,
                            Code = ByteString.CopyFrom(code),
                            Name = TokenSmartContractAddressNameProvider.Name,
                            //TransactionMethodCallList = GenerateTokenInitializationCallList()
                        })).Output;
                
                AsyncHelper.RunSync(() =>
                    SetContractCacheAsync(TokenContractAddress, Hash.FromRawBytes(code)));
                TokenContractStub = GetTokenContractTester(DefaultSenderKeyPair);
                AsyncHelper.RunSync(InitializeTokenAsync);
            }
        }

        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }
        
        internal AssociationAuthContractContainer.AssociationAuthContractStub GetAssociationAuthContractTester(ECKeyPair keyPair)
        {
            return GetTester<AssociationAuthContractContainer.AssociationAuthContractStub>(AssociationAuthContractAddress, keyPair);
        }
        
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateTokenInitializationCallList()
        {
            const string symbol = "ELF";
            const long totalSupply = 100_000_000;
            var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContract.Create), new CreateInput
            {
                Symbol = symbol,
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = totalSupply,
                Issuer = DefaultSender
            });
            //issue default user
            tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
            {
                Symbol = symbol,
                Amount = totalSupply - 20 * 100_000L,
                To = DefaultSender,
                Memo = "Issue token to default user",
            });
            return tokenContractCallList;
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
                Issuer = DefaultSender
            });
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = symbol,
                Amount = totalSupply - 20 * 100_000L,
                To = DefaultSender,
                Memo = "Issue token to default user"
            });
        }
    }
}