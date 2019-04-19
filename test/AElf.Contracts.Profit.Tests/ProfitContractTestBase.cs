using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AElf.Contracts.Profit
{
    public class ProfitContractTestBase : ContractTestBase<ProfitContractTestAElfModule>
    {
        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected Address TokenContractAddress { get; set; }
        protected Address ProfitContractAddress { get; set; }

        internal List<ProfitContractContainer.ProfitContractStub> Creators => CreatorMinerKeyPair
            .Select(p => GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, p)).ToList();

        protected List<ECKeyPair> CreatorMinerKeyPair => SampleECKeyPairs.KeyPairs.Take(4).ToList();

        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }

        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }

        internal ProfitContractContainer.ProfitContractStub ProfitContractStub { get; set; }

        protected void InitializeContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);

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
            
            ProfitContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ProfitContract).Assembly.Location)),
                        Name = ProfitSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateProfitInitializationCallList()
                    })).Output;
            ProfitContractStub = GetProfitContractTester(DefaultSenderKeyPair);
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

        private SystemTransactionMethodCallList GenerateProfitInitializationCallList()
        {
            var voteMethodCallList = new SystemTransactionMethodCallList();
            voteMethodCallList.Add(nameof(ProfitContract.InitializeProfitContract),
                new InitializeProfitContractInput
                {
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                });

            return voteMethodCallList;
        }

        private SystemTransactionMethodCallList GenerateTokenInitializationCallList()
        {
            const string symbol = "ELF";
            const long totalSupply = 100_000_000;
            var tokenContractCallList = new SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContract.CreateNativeToken), new CreateNativeTokenInput
            {
                Symbol = symbol,
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = totalSupply,
                Issuer = DefaultSender,
                LockWhiteSystemContractNameList =
                {
                    VoteSmartContractAddressNameProvider.Name
                }
            });

            // For creating `Treasury` profit item.
            tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
            {
                Symbol = symbol,
                Amount = (long) (totalSupply * 0.2),
                To = ContractZeroAddress,
                Memo = "Issue token to default user for vote.",
            });

            CreatorMinerKeyPair.ForEach(creatorKeyPair => tokenContractCallList.Add(nameof(TokenContract.Issue),
                new IssueInput
                {
                    Symbol = symbol,
                    Amount = (long) (totalSupply * 0.2),
                    To = Address.FromPublicKey(creatorKeyPair.PublicKey),
                    Memo = "set voters few amount for voting."
                }));
            

            return tokenContractCallList;
        }
    }
}