using System.Linq;
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

namespace AElf.Contracts.Association
{
    public class AssociationContractTestBase : ContractTestBase<AssociationContractTestAElfModule>
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

        private byte[] AssociationContractCode => Codes.Single(kv => kv.Key.Contains("Association")).Value;
        private byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;

        protected void DeployContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);

            //deploy Association contract
            AssociationContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySmartContract.SendAsync(
                    new ContractDeploymentInput()
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(AssociationContractCode)
                    })).Output;
            AssociationContractStub = GetAssociationContractTester(DefaultSenderKeyPair);
            
            TokenContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(TokenContractCode),
                        Name = TokenSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateTokenInitializationCallList()
                    })).Output;
            TokenContractStub = GetTokenContractTester(DefaultSenderKeyPair);
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
        
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateTokenInitializationCallList()
        {
            const string symbol = "ELF";
            const long totalSupply = 100_000_000;
            var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContractStub.Create), new CreateInput
            {
                Symbol = symbol,
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = totalSupply,
                Issuer = DefaultSender
            });
            //issue default user
            tokenContractCallList.Add(nameof(TokenContractStub.Issue), new IssueInput
            {
                Symbol = symbol,
                Amount = totalSupply - 20 * 100_000L,
                To = DefaultSender,
                Memo = "Issue token to default user",
            });
            return tokenContractCallList;
        }
    }
}