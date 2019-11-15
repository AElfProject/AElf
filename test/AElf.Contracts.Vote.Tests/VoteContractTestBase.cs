using System.IO;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.Threading;
using InitializeInput = AElf.Contracts.ParliamentAuth.InitializeInput;

namespace AElf.Contracts.Vote
{
    public class VoteContractTestBase : ContractTestBase<VoteContractTestAElfModule>
    {
        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected Address TokenContractAddress { get; set; }
        protected Address VoteContractAddress { get; set; }
        protected Address ParliamentAuthContractAddress { get; set; }
        protected new Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        internal VoteContractContainer.VoteContractStub VoteContractStub { get; set; }
        internal ParliamentAuthContractContainer.ParliamentAuthContractStub ParliamentAuthContractStub { get; set; }

        protected const string TestTokenSymbol = "ELF";

        protected void InitializeContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);

            //deploy vote contract
            var voteContractCode = File.ReadAllBytes(typeof(VoteContract).Assembly.Location);
            VoteContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(voteContractCode),
                        Name = VoteSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateVoteInitializationCallList()
                    })).Output;
            AsyncHelper.RunSync(() => SetContractCacheAsync(VoteContractAddress, Hash.FromRawBytes(voteContractCode)));
            VoteContractStub = GetVoteContractTester(DefaultSenderKeyPair);
            
            //deploy token contract
            var tokenContractCode = File.ReadAllBytes(typeof(TokenContract).Assembly.Location);
            TokenContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(tokenContractCode),
                        Name = TokenSmartContractAddressNameProvider.Name
                    })).Output;
            AsyncHelper.RunSync(() => SetContractCacheAsync(TokenContractAddress, Hash.FromRawBytes(tokenContractCode)));
            TokenContractStub = GetTokenContractTester(DefaultSenderKeyPair);
            AsyncHelper.RunSync(InitializeTokenContractAsync);
            
            //deploy parliament auth contract
            var parliamentAuthContractCode = File.ReadAllBytes(typeof(ParliamentAuthContract).Assembly.Location);
            ParliamentAuthContractAddress = AsyncHelper.RunSync(()=>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(parliamentAuthContractCode),
                        Name = ParliamentAuthSmartContractAddressNameProvider.Name
                    })).Output;
            AsyncHelper.RunSync(() => SetContractCacheAsync(ParliamentAuthContractAddress, Hash.FromRawBytes(parliamentAuthContractCode)));
            ParliamentAuthContractStub = GetParliamentAuthContractTester(DefaultSenderKeyPair);
            AsyncHelper.RunSync(InitializeParliamentAuthContractAsync);
        }

        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }

        internal VoteContractContainer.VoteContractStub GetVoteContractTester(ECKeyPair keyPair)
        {
            return GetTester<VoteContractContainer.VoteContractStub>(VoteContractAddress, keyPair);
        }
        
        internal ParliamentAuthContractContainer.ParliamentAuthContractStub GetParliamentAuthContractTester(ECKeyPair keyPair)
        {
            return GetTester<ParliamentAuthContractContainer.ParliamentAuthContractStub>(ParliamentAuthContractAddress, keyPair);
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateVoteInitializationCallList()
        {
            return new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateTokenInitializationCallList()
        {
            const long totalSupply = 1_000_000_000_0000_0000;
            
            var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContract.Create), new CreateInput
            {
                Symbol = TestTokenSymbol,
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token for testing",
                TotalSupply = totalSupply,
                Issuer = DefaultSender,
                LockWhiteList =
                {
                     VoteContractAddress
                }
            });

            //issue default user
            tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
            {
                Symbol = TestTokenSymbol,
                Amount = totalSupply - 20 * 100_000_0000_0000L,
                To = DefaultSender,
                Memo = "Issue token to default user for vote.",
            });
            
            //issue some amount to voter
            for (int i = 1; i < 20; i++)
            {
                tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
                {
                    Symbol = TestTokenSymbol,
                    Amount = 100_000_0000_0000L,
                    To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[i].PublicKey),
                    Memo = "set voters few amount for voting."
                });
            }

            return tokenContractCallList;
        }

        private async Task InitializeTokenContractAsync()
        {
            const long totalSupply = 1_000_000_000_0000_0000;

            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = TestTokenSymbol,
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token for testing",
                TotalSupply = totalSupply,
                Issuer = DefaultSender,
                LockWhiteList =
                {
                    VoteContractAddress
                }
            });

            //issue default user
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = TestTokenSymbol,
                Amount = totalSupply - 20 * 100_000_0000_0000L,
                To = DefaultSender,
                Memo = "Issue token to default user for vote.",
            });
            
            //issue some amount to voter
            for (int i = 1; i < 20; i++)
            {
                await TokenContractStub.Issue.SendAsync(new IssueInput
                {
                    Symbol = TestTokenSymbol,
                    Amount = 100_000_0000_0000L,
                    To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[i].PublicKey),
                    Memo = "set voters few amount for voting."
                });
            }
        }
        
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateParliamentInitializationCallList()
        {
            var parliamentContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            parliamentContractCallList.Add(nameof(ParliamentAuthContract.Initialize), new InitializeInput
            {
                GenesisOwnerReleaseThreshold = 1,
                PrivilegedProposer = DefaultSender,
                ProposerAuthorityRequired = true
            });

            return parliamentContractCallList;
        }

        private async Task InitializeParliamentAuthContractAsync()
        {
            await ParliamentAuthContractStub.Initialize.SendAsync(new InitializeInput
            {
                GenesisOwnerReleaseThreshold = 1,
                PrivilegedProposer = DefaultSender,
                ProposerAuthorityRequired = true
            });

        }

        protected long GetUserBalance(Address owner)
        {
            return TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = owner,
                Symbol = TestTokenSymbol
            }).Result.Balance;
        }
    }
}