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
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Contracts.ReferendumAuth
{
    public class ReferendumAuthContractTestBase : ContractTestBase<ReferendumAuthContractTestAElfModule>
    {
        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        
        protected Address TokenContractAddress { get; set; }
        protected Address ReferendumAuthContractAddress { get; set; }
        protected new Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
        
        protected IBlockTimeProvider BlockTimeProvider =>
            Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();

        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        internal ReferendumAuthContractContainer.ReferendumAuthContractStub ReferendumAuthContractStub { get; set; }

        protected void InitializeContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);
            
            //deploy ReferendumAuth contract
            var referendumAuthContractCode = File.ReadAllBytes(typeof(ReferendumAuthContract).Assembly.Location);
            ReferendumAuthContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(new SystemContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(referendumAuthContractCode),
                    Name = Hash.FromString("AElf.ContractNames.ReferendumAuth"),
                })).Output;
            AsyncHelper.RunSync(() =>
                SetContractCacheAsync(ReferendumAuthContractAddress, Hash.FromRawBytes(referendumAuthContractCode)));
            ReferendumAuthContractStub = GetReferendumAuthContractTester(DefaultSenderKeyPair);
            AsyncHelper.RunSync(InitializeReferendumAuthContractAsync);

            var tokenContractCode = File.ReadAllBytes(typeof(TokenContract).Assembly.Location);
            TokenContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(tokenContractCode),
                        Name = TokenSmartContractAddressNameProvider.Name
                    })).Output;
            AsyncHelper.RunSync(() =>
                SetContractCacheAsync(TokenContractAddress, Hash.FromRawBytes(tokenContractCode)));
            TokenContractStub = GetTokenContractTester(DefaultSenderKeyPair);
            AsyncHelper.RunSync(InitializeTokenContractAsync);
        }

        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }

        internal ReferendumAuthContractContainer.ReferendumAuthContractStub GetReferendumAuthContractTester(ECKeyPair keyPair)
        {
            return GetTester<ReferendumAuthContractContainer.ReferendumAuthContractStub>(ReferendumAuthContractAddress, keyPair);
        }
        
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateReferendumAuthInitializationCallList()
        {
            var referendumAuthContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            referendumAuthContractCallList.Add(nameof(ReferendumAuthContract.Initialize), new Empty());
            return referendumAuthContractCallList;
        }

        private async Task InitializeReferendumAuthContractAsync()
        {
            await ReferendumAuthContractStub.Initialize.SendAsync(new Empty());
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
                Issuer = DefaultSender,
                LockWhiteList =
                {
                    ReferendumAuthContractAddress
                }
            });

            //issue default user
            tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
            {
                Symbol = symbol,
                Amount = totalSupply - 20 * 100_000L,
                To = DefaultSender,
                Memo = "Issue token to default user.",
            });
            
            //issue some user
            for (int i = 1; i <6; i++)
            {
                tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
                {
                    Symbol = symbol,
                    Amount = 10000,
                    To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[i].PublicKey),
                    Memo = "Issue token to users"
                });
            }
            return tokenContractCallList;
        }

        private async Task InitializeTokenContractAsync()
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
                LockWhiteList =
                {
                    ReferendumAuthContractAddress
                }
            });

            //issue default user
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = symbol,
                Amount = totalSupply - 20 * 100_000L,
                To = DefaultSender,
                Memo = "Issue token to default user.",
            });
            
            //issue some user
            for (int i = 1; i <6; i++)
            {
                await TokenContractStub.Issue.SendAsync(new IssueInput
                {
                    Symbol = symbol,
                    Amount = 10000,
                    To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[i].PublicKey),
                    Memo = "Issue token to users"
                });
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