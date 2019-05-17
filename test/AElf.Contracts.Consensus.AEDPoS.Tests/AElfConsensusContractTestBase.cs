using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.TestKit;
using AElf.Contracts.Vote;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Account.Infrastructure;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Token;
using AElf.Sdk.CSharp;
using AElf.Types;
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

        protected static int SmallBlockMiningInterval = MiningInterval.Div(AEDPoSContractConstants.TinyBlocksNumber);

        protected const long TimeEachTerm = 604800;// 7 * 60 * 60 * 24

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

//        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }

        internal ElectionContractContainer.ElectionContractStub ElectionContractStub { get; set; }

        internal AEDPoSContractContainer.AEDPoSContractStub AElfConsensusContractStub { get; set; }
        
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        
        internal VoteContractContainer.VoteContractStub VoteContractStub { get; set; }
        internal ProfitContractContainer.ProfitContractStub ProfitContractStub { get; set; }

        private byte[] ConsensusContractCode => Codes.Single(kv => kv.Key.Contains("AEDPoS")).Value;
        private byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
        private byte[] ElectionContractCode => Codes.Single(kv => kv.Key.Contains("Election")).Value;
        private byte[] ProfitContractCode => Codes.Single(kv => kv.Key.Contains("Profit")).Value;
        private byte[] VoteContractCode => Codes.Single(kv => kv.Key.Contains("Vote")).Value;       
        protected DateTime BlockchainStartTime => DateTime.Parse("2019-01-01 00:00:00.000").ToUniversalTime();

        protected void InitializeContracts()
        {
            ECKeyPairProvider.SetKeyPair(BootMinerKeyPair);

//            BasicContractZeroStub = GetContractZeroTester(BootMinerKeyPair);

            // Deploy Vote Contract
            VoteContractAddress = AsyncHelper.RunSync(  () =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    VoteContractCode,
                    VoteSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
//                        TransactionMethodCallList = GenerateVoteInitializationCallList()
//                    })).Output;
            VoteContractStub = GetVoteContractTester(BootMinerKeyPair);
            AsyncHelper.RunSync(InitializeVote);
            
            // Deploy Profit Contract
            ProfitContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    ProfitContractCode,
                    ProfitSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
//                        TransactionMethodCallList = GenerateProfitInitializationCallList()
//        })).Output;
            ProfitContractStub =
                GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, BootMinerKeyPair);
            AsyncHelper.RunSync(InitializeProfit);
            
            // Deploy Election Contract.
            ElectionContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    ElectionContractCode,
                    ElectionSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
//                        TransactionMethodCallList = GenerateElectionInitializationCallList()
//                    })).Output;
            ElectionContractStub = GetElectionContractTester(BootMinerKeyPair);
            AsyncHelper.RunSync(InitializeElection);
            
            // Deploy Token Contract
            TokenContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    TokenContractCode,
                    TokenSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
//                        TransactionMethodCallList = GenerateTokenInitializationCallList()
//                    })).Output;
            TokenContractStub = GetTokenContractTester(BootMinerKeyPair);
            AsyncHelper.RunSync(InitializeToken);

            // Deploy AElf Consensus Contract.
            AElfConsensusContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                ConsensusContractCode,
                ConsensusSmartContractAddressNameProvider.Name,
                BootMinerKeyPair));
//                        TransactionMethodCallList = GenerateAElfConsensusInitializationCallList()
//                    })).Output;
            AElfConsensusContractStub = GetAElfConsensusContractTester(BootMinerKeyPair);
            AsyncHelper.RunSync(InitializeAElfConsensus);
        }

//        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
//        {
//            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
//        }

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

        private void CheckResult(TransactionResult result)
        {
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new Exception(result.Error);
            }
        }
        private async Task InitializeVote()
        {
            var result = await VoteContractStub.InitialVoteContract.SendAsync(
                new InitialVoteContractInput
                {
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                });
            CheckResult(result.TransactionResult);
        }
//        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateVoteInitializationCallList()
//        {
//            var voteMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
//            voteMethodCallList.Add(nameof(VoteContract.InitialVoteContract),
//                new InitialVoteContractInput
//                {
//                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
//                });
//
//            return voteMethodCallList;
//        }

        private async Task InitializeElection()
        {
//            var electionMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            var result = await ElectionContractStub.InitialElectionContract.SendAsync(new InitialElectionContractInput
            {
                // Create Treasury profit item and register sub items.
                TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                VoteContractSystemName = VoteSmartContractAddressNameProvider.Name,
                ProfitContractSystemName = ProfitSmartContractAddressNameProvider.Name,
                    
                // Get current miners.
                ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name,
                MaximumLockTime = 1080 * 86400,
                MinimumLockTime = 90 * 86400
            });
            CheckResult(result.TransactionResult);
        }
        
//        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateElectionInitializationCallList()
//        {
//            var electionMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
//            electionMethodCallList.Add(nameof(ElectionContract.InitialElectionContract),
//                new InitialElectionContractInput
//                {
//                    // Create Treasury profit item and register sub items.
//                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
//                    VoteContractSystemName = VoteSmartContractAddressNameProvider.Name,
//                    ProfitContractSystemName = ProfitSmartContractAddressNameProvider.Name,
//                    
//                    // Get current miners.
//                    ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name,
//                    MaximumLockTime = 1080 * 86400,
//                    MinimumLockTime = 90 * 86400
//                });
//            return electionMethodCallList;
//        }

        private async Task InitializeAElfConsensus()
        {
//            var aelfConsensusMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            var result1 = await AElfConsensusContractStub.InitialAElfConsensusContract.SendAsync(
                new InitialAElfConsensusContractInput
                {
                    ElectionContractSystemName = ElectionSmartContractAddressNameProvider.Name,
                    VoteContractSystemName = VoteSmartContractAddressNameProvider.Name,
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                    TimeEachTerm = TimeEachTerm
                });
            CheckResult(result1.TransactionResult);
            var result2 = await AElfConsensusContractStub.FirstRound.SendAsync(
                new MinerList
                    {
                        PublicKeys = {InitialMinersKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey))}
                    }.GenerateFirstRoundOfNewTerm(MiningInterval, StartTimestamp.ToDateTime()));
            CheckResult(result2.TransactionResult);
        }

        private async Task InitializeToken()
        {
            const string symbol = "ELF";
            const long totalSupply = 1000_000_0000; //1000_000_000
            var result1 = await TokenContractStub.CreateNativeToken.SendAsync(new CreateNativeTokenInput
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
            CheckResult(result1.TransactionResult);
            
            var result2 = await TokenContractStub.IssueNativeToken.SendAsync( new IssueNativeTokenInput
            {
                Symbol = symbol,
                Amount = (long) (totalSupply * 0.2),
                ToSystemContractName = ElectionSmartContractAddressNameProvider.Name,
                Memo = "Set dividends.",
            });
            CheckResult(result2.TransactionResult);
            
            var result3 = await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = symbol,
                Amount = (long)(totalSupply * 0.6),
                To = BootMinerAddress,
                Memo = "Issue token to default user.",
            });
            CheckResult(result3.TransactionResult);

            foreach (var initialMinerKeyPair in InitialMinersKeyPairs)
            {
                var result4 = await TokenContractStub.Issue.SendAsync(new IssueInput
                {
                    Symbol = symbol,
                    Amount = (long) (totalSupply * 0.2) / InitialMinersCount,
                    To = Address.FromPublicKey(initialMinerKeyPair.PublicKey),
                    Memo = "Set initial miner's balance.",
                });
                CheckResult(result4.TransactionResult);
            }
        }
//        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateTokenInitializationCallList()
//        {
//            const string symbol = "ELF";
//            const long totalSupply = 1000_000_0000; //1000_000_000
//            var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
//            tokenContractCallList.Add(nameof(TokenContract.CreateNativeToken), new CreateNativeTokenInput
//            {
//                Symbol = symbol,
//                Decimals = 2,
//                IsBurnable = true,
//                TokenName = "elf token",
//                TotalSupply = totalSupply,
//                Issuer = BootMinerAddress,
//                LockWhiteSystemContractNameList =
//                {
//                    ElectionSmartContractAddressNameProvider.Name,
//                    VoteSmartContractAddressNameProvider.Name,
//                    ProfitSmartContractAddressNameProvider.Name,
//                }
//            });
//            
//            tokenContractCallList.Add(nameof(TokenContract.IssueNativeToken), new IssueNativeTokenInput
//            {
//                Symbol = symbol,
//                Amount = (long) (totalSupply * 0.2),
//                ToSystemContractName = ElectionSmartContractAddressNameProvider.Name,
//                Memo = "Set dividends.",
//            });
//
//            tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
//            {
//                Symbol = symbol,
//                Amount = (long)(totalSupply * 0.6),
//                To = BootMinerAddress,
//                Memo = "Issue token to default user.",
//            });
//
//            foreach (var initialMinerKeyPair in InitialMinersKeyPairs)
//            {
//                tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
//                {
//                    Symbol = symbol,
//                    Amount = (long) (totalSupply * 0.2) / InitialMinersCount,
//                    To = Address.FromPublicKey(initialMinerKeyPair.PublicKey),
//                    Memo = "Set initial miner's balance.",
//                });
//            }
//            
//            // Set fee pool address to election contract address.
//            tokenContractCallList.Add(nameof(TokenContract.SetFeePoolAddress),
//                ElectionSmartContractAddressNameProvider.Name);
//
//            return tokenContractCallList;
//        }
        
        private async Task InitializeProfit()
        {
            var result = await ProfitContractStub.InitializeProfitContract.SendAsync(new InitializeProfitContractInput
            {
                // To handle tokens when release profit, add profits and receive profits.
                TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
            });
            CheckResult(result.TransactionResult);
        }
//        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateProfitInitializationCallList()
//        {
//        var profitContractMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
//        profitContractMethodCallList.Add(nameof(ProfitContract.InitializeProfitContract),
//        new InitializeProfitContractInput
//        {
//        // To handle tokens when release profit, add profits and receive profits.
//        TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
//        });
//        return profitContractMethodCallList;
//        } 
    }
}