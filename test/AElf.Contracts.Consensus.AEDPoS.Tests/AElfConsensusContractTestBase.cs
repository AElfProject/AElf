using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.TestKit;
using AElf.Contracts.Vote;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Account.Infrastructure;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public class AElfConsensusContractTestBase : ContractTestBase<AEDPoSContractTestAElfModule>
    {
        protected IBlockTimeProvider BlockTimeProvider =>
            Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();
        
        protected IAElfAsymmetricCipherKeyPairProvider ECKeyPairProvider =>
            Application.ServiceProvider.GetRequiredService<IAElfAsymmetricCipherKeyPairProvider>();
        
        protected const int InitialMinersCount = 5;
        
        protected const int CandidatesCount = 10;
        
        protected const int VotersCount = 10;

        protected const int MiningInterval = 4000;

        protected const int SmallBlockMiningInterval = 500;

        protected const long DaysEachTerm = 7;

        protected static readonly Timestamp StartTimestamp = DateTime.UtcNow.ToTimestamp();

        protected ECKeyPair BootMinerKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address BootMinerAddress => Address.FromPublicKey(BootMinerKeyPair.PublicKey);
        protected Address AElfConsensusContractAddress { get; set; }
        protected Address ElectionContractAddress { get; set; }
        protected Address TokenContractAddress { get; set; }
        protected Address ProfitContractAddress { get; set; }
        protected Address VoteContractAddress { get; set; }

        internal List<AEDPoSContractContainer.AEDPoSContractStub> InitialMiners => InitialMinersKeyPairs
            .Select(p =>
                GetTester<AEDPoSContractContainer.AEDPoSContractStub>(AElfConsensusContractAddress, p))
            .ToList();

        internal AEDPoSContractContainer.AEDPoSContractStub BootMiner => InitialMiners[0];

        internal List<AEDPoSContractContainer.AEDPoSContractStub> BackupNodes => BackupNodesKeyPair
            .Select(p =>
                GetTester<AEDPoSContractContainer.AEDPoSContractStub>(AElfConsensusContractAddress, p))
            .ToList();

        protected List<ECKeyPair> InitialMinersKeyPairs => SampleECKeyPairs.KeyPairs.Take(InitialMinersCount).ToList();
        protected List<ECKeyPair> CandidatesKeyPairs =>
            SampleECKeyPairs.KeyPairs.Skip(InitialMinersCount).Take(CandidatesCount).ToList();
        
        protected List<ECKeyPair> VotersKeyPairs =>
            SampleECKeyPairs.KeyPairs.Skip(InitialMinersCount + CandidatesCount).Take(VotersCount).ToList();
        
        protected List<ECKeyPair> BackupNodesKeyPair =>
            SampleECKeyPairs.KeyPairs.Skip(InitialMinersCount).Take(InitialMinersCount).ToList();

        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }

        internal ElectionContractContainer.ElectionContractStub ElectionContractStub { get; set; }

        internal AEDPoSContractContainer.AEDPoSContractStub AElfConsensusContractStub { get; set; }
        
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        
        internal VoteContractContainer.VoteContractStub VoteContractStub { get; set; }
        
        protected DateTime BlockchainStartTime => DateTime.Parse("2019-01-01 00:00:00.000").ToUniversalTime();

        protected void InitializeContracts()
        {
            ECKeyPairProvider.SetKeyPair(BootMinerKeyPair);

            BasicContractZeroStub = GetContractZeroTester(BootMinerKeyPair);

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
            
            // Deploy Profit Contract
            ProfitContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ProfitContract).Assembly.Location)),
                        Name = ProfitSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateProfitInitializationCallList()
                    })).Output;
            
            // Deploy Election Contract.
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
            TokenContractAddress = AsyncHelper.RunSync(() => GetContractZeroTester(BootMinerKeyPair)
                .DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location)),
                        Name = TokenSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateTokenInitializationCallList()
                    })).Output;
            TokenContractStub = GetTokenContractTester(BootMinerKeyPair);

            // Deploy AElf Consensus Contract.
            AElfConsensusContractAddress = AsyncHelper.RunSync(() => GetContractZeroTester(BootMinerKeyPair)
                .DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(AEDPoSContract).Assembly.Location)),
                        Name = ConsensusSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateAElfConsensusInitializationCallList()
                    })).Output;
            AElfConsensusContractStub = GetAElfConsensusContractTester(BootMinerKeyPair);
        }

        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }

        internal ElectionContractContainer.ElectionContractStub GetElectionContractTester(ECKeyPair keyPair)
        {
            return GetTester<ElectionContractContainer.ElectionContractStub>(ElectionContractAddress, keyPair);
        }

        internal AEDPoSContractContainer.AEDPoSContractStub GetAElfConsensusContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractContainer.AEDPoSContractStub>(AElfConsensusContractAddress,
                keyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress,
                keyPair);
        }

        internal VoteContractContainer.VoteContractStub GetVoteContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<VoteContractContainer.VoteContractStub>(VoteContractAddress,
                keyPair);
        }

        protected async Task BootMinerChangeRoundAsync(long nextRoundNumber = 2)
        {
            var currentRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());
            var expectedStartTime = StartTimestamp.ToDateTime()
                .AddMilliseconds(((long) currentRound.TotalMilliseconds(MiningInterval)).Mul(nextRoundNumber.Sub(1)));
            currentRound.GenerateNextRoundInformation(expectedStartTime, StartTimestamp, out var nextRound);
            await BootMiner.NextRound.SendAsync(nextRound);
        }
        
        protected DateTime GetRoundExpectedStartTime(DateTime blockchainStartTime, int roundTotalMilliseconds,
            long roundNumber)
        {
            return blockchainStartTime.AddMilliseconds(roundTotalMilliseconds * (roundNumber - 1));
        }
        
        protected async Task InitializeCandidates(int take = CandidatesCount)
        {
            var initialMiner = GetTokenContractTester(BootMinerKeyPair);
            foreach (var candidatesKeyPair in CandidatesKeyPairs.Take(take))
            {
                await initialMiner.Transfer.SendAsync(new TransferInput
                {
                    Symbol = "ELF",
                    To = Address.FromPublicKey(candidatesKeyPair.PublicKey),
                    Amount = 10_0000
                });
                await GetElectionContractTester(candidatesKeyPair).AnnounceElection.SendAsync(new Empty());
            }
        }
        
        protected async Task InitializeVoters()
        {
            var initialMiner = GetTokenContractTester(BootMinerKeyPair);
            foreach (var voterKeyPair in VotersKeyPairs)
            {
                await initialMiner.Transfer.SendAsync(new TransferInput
                {
                    Symbol = "ELF",
                    To = Address.FromPublicKey(voterKeyPair.PublicKey),
                    Amount = 10_0000,
                    Memo = "transfer token for voter candidate."
                });
            }
        }
        
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateVoteInitializationCallList()
        {
            var voteMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            voteMethodCallList.Add(nameof(VoteContract.InitialVoteContract),
                new InitialVoteContractInput
                {
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                });

            return voteMethodCallList;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateElectionInitializationCallList()
        {
            var electionMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            electionMethodCallList.Add(nameof(ElectionContract.InitialElectionContract),
                new InitialElectionContractInput
                {
                    // Create Treasury profit item and register sub items.
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                    VoteContractSystemName = VoteSmartContractAddressNameProvider.Name,
                    ProfitContractSystemName = ProfitSmartContractAddressNameProvider.Name,
                    
                    // Get current miners.
                    ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name,
                    MaximumLockTime = 1080,
                    MinimumLockTime = 90
                });
            return electionMethodCallList;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateAElfConsensusInitializationCallList()
        {
            var aelfConsensusMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            aelfConsensusMethodCallList.Add(nameof(AEDPoSContract.InitialAElfConsensusContract),
                new InitialAElfConsensusContractInput
                {
                    ElectionContractSystemName = ElectionSmartContractAddressNameProvider.Name,
                    VoteContractSystemName = VoteSmartContractAddressNameProvider.Name,
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                    TimeEachTerm = (int)DaysEachTerm,
                    BaseTimeUnit = 2 // TODO: Remove this after testing.
                });
            aelfConsensusMethodCallList.Add(nameof(AEDPoSContract.FirstRound),
                new MinerList
                    {
                        PublicKeys = {InitialMinersKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey))}
                    }.GenerateFirstRoundOfNewTerm(MiningInterval, StartTimestamp.ToDateTime()));
            return aelfConsensusMethodCallList;
        }
        
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateTokenInitializationCallList()
        {
            const string symbol = "ELF";
            const long totalSupply = 1000_000_0000; //1000_000_000
            var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContract.CreateNativeToken), new CreateNativeTokenInput
            {
                Symbol = symbol,
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = totalSupply,
                Issuer = BootMinerAddress,
                LockWhiteSystemContractNameList =
                {
                    ElectionSmartContractAddressNameProvider.Name,
                    VoteSmartContractAddressNameProvider.Name,
                    ProfitSmartContractAddressNameProvider.Name,
                }
            });
            
            tokenContractCallList.Add(nameof(TokenContract.IssueNativeToken), new IssueNativeTokenInput
            {
                Symbol = symbol,
                Amount = (long) (totalSupply * 0.2),
                ToSystemContractName = ElectionSmartContractAddressNameProvider.Name,
                Memo = "Set dividends.",
            });

            tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
            {
                Symbol = symbol,
                Amount = (long)(totalSupply * 0.6),
                To = BootMinerAddress,
                Memo = "Issue token to default user.",
            });

            foreach (var initialMinerKeyPair in InitialMinersKeyPairs)
            {
                tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
                {
                    Symbol = symbol,
                    Amount = (long) (totalSupply * 0.2) / InitialMinersCount,
                    To = Address.FromPublicKey(initialMinerKeyPair.PublicKey),
                    Memo = "Set initial miner's balance.",
                });
            }
            
            // Set fee pool address to election contract address.
            tokenContractCallList.Add(nameof(TokenContract.SetFeePoolAddress),
                ElectionSmartContractAddressNameProvider.Name);

            return tokenContractCallList;
        }
        
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateProfitInitializationCallList()
        {
        var profitContractMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
        profitContractMethodCallList.Add(nameof(ProfitContract.InitializeProfitContract),
        new InitializeProfitContractInput
        {
        // To handle tokens when release profit, add profits and receive profits.
        TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
        });
        return profitContractMethodCallList;
        } 
    }
}