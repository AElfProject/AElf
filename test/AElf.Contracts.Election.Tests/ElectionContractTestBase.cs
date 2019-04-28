using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using Miners = AElf.Consensus.AElfConsensus.Miners;

namespace AElf.Contracts.Election
{
    public class ElectionContractTestBase : ContractTestBase<ElectionContractTestModule>
    {
        protected IBlockTimeProvider BlockTimeProvider =>
            Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();

        protected Timestamp StartTimestamp => DateTime.UtcNow.ToTimestamp();

        protected ECKeyPair BootMinerKeyPair => SampleECKeyPairs.KeyPairs[0];

        internal List<ECKeyPair> InitialMinersKeyPairs => SampleECKeyPairs.KeyPairs.Take(InitialMinersCount).ToList();
        
        internal List<ECKeyPair> FullNodesKeyPairs => SampleECKeyPairs.KeyPairs.Skip(InitialMinersCount).Take(FullNodesCount).ToList();
        
        internal List<ECKeyPair> VotersKeyPairs => SampleECKeyPairs.KeyPairs.Skip(InitialMinersCount + FullNodesCount).Take(VotersCount).ToList();

        internal Dictionary<ProfitType, Hash> ProfitItemsIds { get; set; }
        protected ConsensusOptions ConsensusOption { get; set; }

        internal const int MiningInterval = 4000;
        
        internal const int InitialMinersCount = 5;
        internal const int FullNodesCount = 10;
        internal const int VotersCount = 10;

        protected Address TokenContractAddress { get; set; }
        protected Address VoteContractAddress { get; set; }
        protected Address ProfitContractAddress { get; set; }
        protected Address ElectionContractAddress { get; set; }
        protected Address ConsensusContractAddress { get; set; }

        // Will use BootMinerKeyPair.
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
            BasicContractZeroStub = GetContractZeroTester(BootMinerKeyPair);

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
            VoteContractStub = GetVoteContractTester(BootMinerKeyPair);
            
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
            ProfitContractStub = GetProfitContractTester(BootMinerKeyPair);
            
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
            ElectionContractStub = GetElectionContractTester(BootMinerKeyPair);

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
            TokenContractStub = GetTokenContractTester(BootMinerKeyPair);
            
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
            AElfConsensusContractStub = GetAElfConsensusContractStub(BootMinerKeyPair);

            var profitIds = AsyncHelper.RunSync(() =>
                ProfitContractStub.GetCreatedProfitItems.CallAsync(
                    new GetCreatedProfitItemsInput
                    {
                        Creator = ElectionContractAddress
                    })).ProfitIds;
            ProfitItemsIds = new Dictionary<ProfitType, Hash>
            {
                {ProfitType.Treasury, profitIds[0]},
                {ProfitType.MinerReward, profitIds[1]},
                {ProfitType.BackSubsidy, profitIds[2]},
                {ProfitType.CitizenWelfare, profitIds[3]},
                {ProfitType.BasicMinerReward, profitIds[4]},
                {ProfitType.VotesWeightReward, profitIds[5]},
                {ProfitType.ReElectionReward, profitIds[6]},
            };
        }

        internal enum ProfitType
        {
            Treasury,
            MinerReward,
            BackSubsidy,
            CitizenWelfare,
            BasicMinerReward,
            VotesWeightReward,
            ReElectionReward
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
            for (var i = 0; i < InitialMinersCount + FullNodesCount + VotersCount; i++)
            {
                if (i < InitialMinersCount)
                {
                    tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
                    {
                        Symbol = ElectionContractTestConsts.NativeTokenSymbol,
                        Amount = ElectionContractConsts.LockTokenForElection * 10,
                        To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[i].PublicKey),
                        Memo = "Initial balance for initial miners."
                    });
                    continue;
                }
                
                if (i < InitialMinersCount + FullNodesCount)
                {
                    tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
                    {
                        Symbol = ElectionContractTestConsts.NativeTokenSymbol,
                        Amount = ElectionContractConsts.LockTokenForElection * 10,
                        To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[i].PublicKey),
                        Memo = "Initial balance for initial full nodes."
                    });
                    continue;
                }
                
                tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
                {
                    Symbol = ElectionContractTestConsts.NativeTokenSymbol,
                    Amount = ElectionContractConsts.LockTokenForElection / 2,
                    To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[i].PublicKey),
                    Memo = "Initial balance for voters."
                });
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

        internal async Task NextTerm(ECKeyPair keyPair)
        {
            var miner = GetAElfConsensusContractStub(keyPair);
            var round = await miner.GetCurrentRoundInformation.CallAsync(new Empty());
            var miners = new Miners
            {
                PublicKeys =
                {
                    round.RealTimeMinersInformation.Keys.Select(k =>
                        ByteString.CopyFrom(ByteArrayHelpers.FromHexString(k)))
                }
            };
            var firstRoundOfNextTerm =
                miners.GenerateFirstRoundOfNewTerm(MiningInterval, BlockTimeProvider.GetBlockTime(), 1, 1);
            await miner.NextTerm.SendAsync(firstRoundOfNextTerm);
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
                InitialMiners = InitialMinersKeyPairs.Select(k => k.PublicKey.ToHex()).ToList(),
                StartTimestamp = StartTimestamp.ToDateTime()
            };
        }
    }
}