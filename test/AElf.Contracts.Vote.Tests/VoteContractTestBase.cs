using System.IO;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AElf.Contracts.Vote
{
    public class VoteContractTestBase : ContractTestBase<VoteContractTestAElfModule>
    {
        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[1];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
        protected Address TokenContractAddress { get; set; }
        protected Address VoteContractAddress { get; set; }

        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }

        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }

        internal VoteContractContainer.VoteContractStub VoteContractStub { get; set; }

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

        private SystemTransactionMethodCallList GenerateVoteInitializationCallList()
        {
            var voteMethodCallList = new SystemTransactionMethodCallList();
            voteMethodCallList.Add(nameof(VoteContract.InitialVoteContract),
                new InitialVoteContractInput
                {
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                    ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name
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
                Symbol = "ELF",
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

            tokenContractCallList.Add(nameof(TokenContract.IssueNativeToken), new IssueNativeTokenInput
            {
                Symbol = symbol,
                Amount = totalSupply,
                Memo = "Issue token to default user for vote.",
                ToSystemContractName = VoteSmartContractAddressNameProvider.Name
            });

            return tokenContractCallList;
        }
    }
}