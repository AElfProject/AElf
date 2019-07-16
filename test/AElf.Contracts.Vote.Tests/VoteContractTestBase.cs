using System.IO;
using Acs0;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
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
            VoteContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(VoteContract).Assembly.Location)),
                        Name = VoteSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateVoteInitializationCallList()
                    })).Output;
            VoteContractStub = GetVoteContractTester(DefaultSenderKeyPair);
            
            //deploy token contract
            TokenContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location)),
                        Name = TokenSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateTokenInitializationCallList()
                    })).Output;
            TokenContractStub = GetTokenContractTester(DefaultSenderKeyPair);
            
            //deploy parliament auth contract
            ParliamentAuthContractAddress = AsyncHelper.RunSync(()=>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ParliamentAuthContract).Assembly.Location)),
                        Name = ParliamentAuthSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateParliamentInitializationCallList()
                    })).Output;
            ParliamentAuthContractStub = GetParliamentAuthContractTester(DefaultSenderKeyPair);
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