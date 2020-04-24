using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Referendum;
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
using AElf.Contracts.Parliament;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.GovernmentSystem;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;

namespace AElf.Contracts.Referendum
{
    public class ReferendumContractTestBase : ContractTestBase<ReferendumContractTestAElfModule>
    {
        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);

        protected static List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
            SampleECKeyPairs.KeyPairs.Take(5).ToList();
        protected Address TokenContractAddress { get; set; }
        protected Address ReferendumContractAddress { get; set; }

        protected Address ParliamentContractAddress { get; set; }

        protected Address ConsensusContractAddress { get; set; }
        protected new Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();

        protected IBlockTimeProvider BlockTimeProvider =>
            Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();

        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        internal ReferendumContractContainer.ReferendumContractStub ReferendumContractStub { get; set; }

        internal ParliamentContractContainer.ParliamentContractStub ParliamentContractStub { get; set; }

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub AEDPoSContractStub { get; set; }
        protected void InitializeContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);

            // deploy token contract
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

            //deploy Referendum contract
            ReferendumContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(new SystemContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ReferendumContract).Assembly.Location)),
                    Name = ReferendumSmartContractAddressNameProvider.Name,
                    // TransactionMethodCallList = GenerateReferendumInitializationCallList()
                })).Output;
            ReferendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
            
            //deploy parliament auth contract
            ParliamentContractAddress = AsyncHelper.RunSync(()=>GetContractZeroTester(DefaultSenderKeyPair)
                .DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ParliamentContract).Assembly.Location)),
                        Name = ParliamentSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateParliamentInitializationCallList()
                    })).Output;
            ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);

            //deploy consensus contract
            ConsensusContractAddress = AsyncHelper.RunSync(()=>GetContractZeroTester(DefaultSenderKeyPair)
                .DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(AEDPoSContract).Assembly.Location)),
                        Name = ConsensusSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateConsensusInitializationCallList()
                    })).Output;
            AEDPoSContractStub = GetConsensusContractTester(DefaultSenderKeyPair);
        }

        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }

        internal ReferendumContractContainer.ReferendumContractStub GetReferendumContractTester(ECKeyPair keyPair)
        {
            return GetTester<ReferendumContractContainer.ReferendumContractStub>(ReferendumContractAddress, keyPair);
        }

        internal ParliamentContractContainer.ParliamentContractStub GetParliamentContractTester(ECKeyPair keyPair)
        {
            return GetTester<ParliamentContractContainer.ParliamentContractStub>(ParliamentContractAddress, keyPair);
        }

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub GetConsensusContractTester(ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(ConsensusContractAddress, keyPair);
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateTokenInitializationCallList()
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
                Memo = "Issue token to default user.",
            });

            //issue some user
            for (int i = 1; i < 6; i++)
            {
                tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
                {
                    Symbol = symbol,
                    Amount = 100_000,
                    To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[i].PublicKey),
                    Memo = "Issue token to users"
                });
            }

            return tokenContractCallList;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateParliamentInitializationCallList()
        {
            var parliamentContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            parliamentContractCallList.Add(nameof(ParliamentContractStub.Initialize), new Parliament.InitializeInput
            {
                PrivilegedProposer = DefaultSender,
                ProposerAuthorityRequired = true
            });

            return parliamentContractCallList;
        }
        
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateConsensusInitializationCallList()
        {
            var consensusContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            consensusContractCallList.Add(nameof(AEDPoSContractStub.InitialAElfConsensusContract), new InitialAElfConsensusContractInput
            {
                PeriodSeconds = 604800L,
                MinerIncreaseInterval = 31536000
            });
            
            consensusContractCallList.Add(nameof(AEDPoSContractStub.FirstRound), new MinerList
            {
                Pubkeys = {InitialCoreDataCenterKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey))}
            }.GenerateFirstRoundOfNewTerm(4000, TimestampHelper.GetUtcNow()));

            return consensusContractCallList;
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