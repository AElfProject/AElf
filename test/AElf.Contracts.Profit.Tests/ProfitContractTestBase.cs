using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace AElf.Contracts.Profit
{
    public class ProfitContractTestBase : ContractTestBase<ProfitContractTestAElfModule>
    {
        protected ECKeyPair StarterKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address Starter => Address.FromPublicKey(StarterKeyPair.PublicKey);
        protected Address TokenContractAddress { get; set; }
        protected Address ProfitContractAddress { get; set; }
        
        protected Address ParliamentAuthAddress{ get; set; }
        
        internal List<ProfitContractContainer.ProfitContractStub> Creators => CreatorKeyPair
            .Select(p => GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, p)).ToList();

        internal List<ProfitContractContainer.ProfitContractStub> Normal => NormalKeyPair
            .Select(p => GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, p)).ToList();
        
        protected List<ECKeyPair> CreatorKeyPair => SampleECKeyPairs.KeyPairs.Skip(1).Take(4).ToList();
        
        protected List<ECKeyPair> NormalKeyPair => SampleECKeyPairs.KeyPairs.Skip(5).Take(5).ToList();

        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }

        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }

        internal ProfitContractContainer.ProfitContractStub ProfitContractStub { get; set; }
        
        internal ParliamentAuthContractContainer.ParliamentAuthContractStub ParliamentContractStub { get; set; }

        protected void InitializeContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(StarterKeyPair);
            
            var profitContractCode = File.ReadAllBytes(typeof(ProfitContract).Assembly.Location);
            ProfitContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(profitContractCode),
                        Name = ProfitSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateProfitInitializationCallList()
                    })).Output;
            AsyncHelper.RunSync(() => SetContractCacheAsync(ProfitContractAddress, Hash.FromRawBytes(profitContractCode)));
            ProfitContractStub = GetProfitContractTester(StarterKeyPair);

            //deploy token contract
            var tokenContractCode = File.ReadAllBytes(typeof(TokenContract).Assembly.Location);
            TokenContractAddress = AsyncHelper.RunSync(() => GetContractZeroTester(StarterKeyPair)
                .DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(tokenContractCode),
                        Name = TokenSmartContractAddressNameProvider.Name
                    })).Output;
            AsyncHelper.RunSync(() => SetContractCacheAsync(TokenContractAddress, Hash.FromRawBytes(tokenContractCode)));
            TokenContractStub = GetTokenContractTester(StarterKeyPair);

            AsyncHelper.RunSync(InitializeTokenContractAsync);
            
            //deploy parliament auth contract
            var parliamentAuthCode = File.ReadAllBytes(typeof(ParliamentAuthContract).Assembly.Location);
            ParliamentAuthAddress = AsyncHelper.RunSync(()=>GetContractZeroTester(StarterKeyPair)
                .DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(parliamentAuthCode),
                        Name = ParliamentAuthSmartContractAddressNameProvider.Name
                    })).Output;
            AsyncHelper.RunSync(() => SetContractCacheAsync(ParliamentAuthAddress, Hash.FromRawBytes(parliamentAuthCode)));
            ParliamentContractStub = GetParliamentContractTester(StarterKeyPair);
            AsyncHelper.RunSync(InitializeParliamentContractAsync);
        }

        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }

        internal ProfitContractContainer.ProfitContractStub GetProfitContractTester(ECKeyPair keyPair)
        {
            return GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, keyPair);
        }
        
        internal ParliamentAuthContractContainer.ParliamentAuthContractStub GetParliamentContractTester(ECKeyPair keyPair)
        {
            return GetTester<ParliamentAuthContractContainer.ParliamentAuthContractStub>(ParliamentAuthAddress, keyPair);
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateProfitInitializationCallList()
        {
            return new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateTokenInitializationCallList()
        {
            const string symbol = "ELF";
            var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContract.Create), new CreateInput
            {
                Symbol = symbol,
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = ProfitContractTestConstants.NativeTokenTotalSupply,
                Issuer = Starter,
                LockWhiteList =
                {
                    ProfitContractAddress
                }
            });

            // For creating `Treasury` profit item.
            tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
            {
                Symbol = symbol,
                Amount = (long) (ProfitContractTestConstants.NativeTokenTotalSupply * 0.2),
                To = Address.FromPublicKey(StarterKeyPair.PublicKey),
                Memo = "Issue token to default user for vote.",
            });

            CreatorKeyPair.ForEach(creatorKeyPair => tokenContractCallList.Add(nameof(TokenContract.Issue),
                new IssueInput
                {
                    Symbol = symbol,
                    Amount = (long) (ProfitContractTestConstants.NativeTokenTotalSupply * 0.1),
                    To = Address.FromPublicKey(creatorKeyPair.PublicKey),
                    Memo = "set voters few amount for voting."
                }));
            
            NormalKeyPair.ForEach(normalKeyPair => tokenContractCallList.Add(nameof(TokenContract.Issue),
                new IssueInput
                {
                    Symbol = symbol,
                    Amount = (long) (ProfitContractTestConstants.NativeTokenTotalSupply * 0.05),
                    To = Address.FromPublicKey(normalKeyPair.PublicKey),
                    Memo = "set voters few amount for voting."
                }));

            return tokenContractCallList;
        }

        private async Task InitializeTokenContractAsync()
        {
            const string symbol = "ELF";
            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = symbol,
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = ProfitContractTestConstants.NativeTokenTotalSupply,
                Issuer = Starter,
                LockWhiteList =
                {
                    ProfitContractAddress
                }
            });

            // For creating `Treasury` profit item.
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = symbol,
                Amount = (long) (ProfitContractTestConstants.NativeTokenTotalSupply * 0.2),
                To = Address.FromPublicKey(StarterKeyPair.PublicKey),
                Memo = "Issue token to default user for vote.",
            });

            foreach (var creatorKeyPair in CreatorKeyPair)
            {
                await TokenContractStub.Issue.SendAsync(new IssueInput
                {
                    Symbol = symbol,
                    Amount = (long) (ProfitContractTestConstants.NativeTokenTotalSupply * 0.1),
                    To = Address.FromPublicKey(creatorKeyPair.PublicKey),
                    Memo = "set voters few amount for voting."
                });
            }

            foreach (var normalKeyPair in NormalKeyPair)
            {
                await TokenContractStub.Issue.SendAsync(new IssueInput
                {
                    Symbol = symbol,
                    Amount = (long) (ProfitContractTestConstants.NativeTokenTotalSupply * 0.05),
                    To = Address.FromPublicKey(normalKeyPair.PublicKey),
                    Memo = "set voters few amount for voting."
                });
            }
        }
        
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateParliamentInitializationCallList()
        {
            var parliamentContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            parliamentContractCallList.Add(nameof(ParliamentContractStub.Initialize), new InitializeInput
            {
                GenesisOwnerReleaseThreshold = 1,
                PrivilegedProposer = Starter,
                ProposerAuthorityRequired = true
            });

            return parliamentContractCallList;
        }

        private async Task InitializeParliamentContractAsync()
        {
            await ParliamentContractStub.Initialize.SendAsync(new InitializeInput
            {
                GenesisOwnerReleaseThreshold = 1,
                PrivilegedProposer = Starter,
                ProposerAuthorityRequired = true
            });
        }
    }
}