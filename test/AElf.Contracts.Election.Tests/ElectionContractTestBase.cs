using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Consensus.AElfConsensus;
using AElf.Consensus.DPoS;
using AElf.Contracts.Consensus.AElfConsensus;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.TestKit;
using AElf.Contracts.Vote;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Contracts.Election
{
    public class ElectionContractTestBase : ContractTestBase<ElectionContractTestModule>
    {
        protected IBlockTimeProvider BlockTimeProvider =>
            Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();

        protected Timestamp StartTimestamp => DateTime.UtcNow.ToTimestamp();

        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);

        protected ConsensusOptions ConsensusOption { get; set; }
        protected Address TokenContractAddress { get; set; }
        protected Address VoteContractAddress { get; set; }
        protected Address ProfitContractAddress { get; set; }
        protected Address ElectionContractAddress { get; set; }
        
        protected Address ConsensusContractAddress { get; set; }

        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }

        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }

        internal VoteContractContainer.VoteContractStub VoteContractStub { get; set; }
        
        internal ProfitContractContainer.ProfitContractStub ProfitContractStub { get; set; }

        internal ElectionContractContainer.ElectionContractStub ElectionContractStub { get; set; }
        
        internal AElfConsensusContractContainer.AElfConsensusContractStub AElfConsensusContractStub { get; set; }

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

        internal ProfitContractContainer.ProfitContractStub GetProfitContractTester(ECKeyPair keyPair)
        {
            return GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, keyPair);
        }
        
        internal ElectionContractContainer.ElectionContractStub GetElectionContractTester(ECKeyPair keyPair)
        {
            return GetTester<ElectionContractContainer.ElectionContractStub>(ElectionContractAddress, keyPair);
        }

        internal AElfConsensusContractContainer.AElfConsensusContractStub GetAElfConsensusContractStub(
            ECKeyPair keyPair)
        {
            return GetTester<AElfConsensusContractContainer.AElfConsensusContractStub>(ConsensusContractAddress, keyPair);
        }

        protected void InitializeContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);

            BlockTimeProvider.SetBlockTime(StartTimestamp.ToDateTime());

            // Deploy Vote Contract
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
            
            //Deploy Profit Contract
            ProfitContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ProfitContract).Assembly.Location)),
                        Name = ProfitSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateProfitInitializationCallList()
                    })).Output;
            
            // Deploy Election Contract
            ElectionContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ElectionContract).Assembly.Location)),
                        Name = ElectionSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateElectionInitializationCallList()
                    })).Output;
            ElectionContractStub = GetElectionContractTester(DefaultSenderKeyPair);

            // Deploy Token Contract
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
            
            //Deploy Consensus Contract
            ConsensusContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(AElfConsensusContract).Assembly.Location)),
                        Name = ConsensusSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateConsensusInitializationCallList()
                    })).Output;
            AElfConsensusContractStub = GetAElfConsensusContractStub(DefaultSenderKeyPair);
        }

        private SystemTransactionMethodCallList GenerateVoteInitializationCallList()
        {
            var voteMethodCallList = new SystemTransactionMethodCallList();
            voteMethodCallList.Add(nameof(VoteContract.InitialVoteContract),
                new InitialVoteContractInput
                {
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                });

            return voteMethodCallList;
        }

        private SystemTransactionMethodCallList GenerateProfitInitializationCallList()
        {
            var profitMethodCallList = new SystemTransactionMethodCallList();
            profitMethodCallList.Add(nameof(ProfitContract.InitializeProfitContract),
                new InitializeProfitContractInput
                {
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name
                });

            return profitMethodCallList;
        }

        private SystemTransactionMethodCallList GenerateTokenInitializationCallList()
        {
            var tokenContractCallList = new SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContract.CreateNativeToken), new CreateNativeTokenInput
            {
                Symbol = "ELF",
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = ElectionContractTestConsts.NativeTokenTotalSupply,
                Issuer = ContractZeroAddress,
                LockWhiteSystemContractNameList =
                {
                    ElectionSmartContractAddressNameProvider.Name,
                    VoteSmartContractAddressNameProvider.Name,
                    ProfitSmartContractAddressNameProvider.Name
                }
            });

            //issue default user
            tokenContractCallList.Add(nameof(TokenContract.IssueNativeToken), new IssueNativeTokenInput
            {
                Symbol = "ELF",
                Amount = ElectionContractTestConsts.NativeTokenTotalSupply / 5,
                ToSystemContractName = ElectionSmartContractAddressNameProvider.Name,
                Memo = "Set dividends.",
            });

            //issue some amount for bp announcement and user vote
            for (int i = 1; i <= 50; i++)
            {
                if (i <= 10)
                {
                    tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
                    {
                        Symbol = ElectionContractTestConsts.NativeTokenSymbol,
                        Amount = 150_000L,
                        To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[i].PublicKey),
                        Memo = "set voters few amount for voting."
                    });
                }
                else
                {
                    tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
                    {
                        Symbol = ElectionContractTestConsts.NativeTokenSymbol,
                        Amount = 50_000L,
                        To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[i].PublicKey),
                        Memo = "set voters few amount for voting."
                    });
                }
            }
            
            //set pool address to election contract address
            tokenContractCallList.Add(nameof(TokenContract.SetFeePoolAddress),
                ElectionSmartContractAddressNameProvider.Name);
            
            return tokenContractCallList;
        }

        private SystemTransactionMethodCallList GenerateElectionInitializationCallList()
        {
            var electionMethodCallList = new SystemTransactionMethodCallList();
            electionMethodCallList.Add(nameof(ElectionContract.InitialElectionContract),
                new InitialElectionContractInput
                {
                    VoteContractSystemName = VoteSmartContractAddressNameProvider.Name,
                    ProfitContractSystemName = ProfitSmartContractAddressNameProvider.Name,
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                    AelfConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name
                });

            return electionMethodCallList;
        }

        private SystemTransactionMethodCallList GenerateConsensusInitializationCallList()
        {
            var consensusMethodList = new SystemTransactionMethodCallList();
            ConsensusOption = GetDefaultConsensusOptions();
            
            consensusMethodList.Add(nameof(AElfConsensusContract.InitialAElfConsensusContract),
                new InitialAElfConsensusContractInput
                {
                    ElectionContractSystemName = ElectionSmartContractAddressNameProvider.Name,
                });
            consensusMethodList.Add(nameof(AElfConsensusContract.FirstRound),
                ConsensusOption.InitialMiners.ToMiners().GenerateFirstRoundOfNewTerm(ConsensusOption.MiningInterval,
                    ConsensusOption.StartTimestamp.ToUniversalTime()));
            return consensusMethodList;
        }

        internal async Task<long> GetNativeTokenBalance(byte[] publicKey)
        {
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = ElectionContractTestConsts.NativeTokenSymbol,
                Owner = Address.FromPublicKey(publicKey)
            })).Balance;

            return balance;
        }
        
        internal async Task<long> GetVoteTokenBalance(byte[] publicKey)
        {
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = ElectionContractTestConsts.VoteTokenSymbol,
                Owner = Address.FromPublicKey(publicKey)
            })).Balance;

            return balance;
        }

        private ConsensusOptions GetDefaultConsensusOptions()
        {
            return new ConsensusOptions
            {
                MiningInterval = 4000,
                InitialMiners = new List<string>
                {
                    SampleECKeyPairs.KeyPairs[0].PublicKey.ToHex(),
                    SampleECKeyPairs.KeyPairs[1].PublicKey.ToHex(),
                    SampleECKeyPairs.KeyPairs[2].PublicKey.ToHex(),
                },
                DaysEachTerm = 7,
                StartTimestamp = StartTimestamp.ToDateTime()
            };
        }
    }
}