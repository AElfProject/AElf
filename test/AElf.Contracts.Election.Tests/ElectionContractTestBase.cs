using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.TestKit;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Contracts.Election
{
    // ReSharper disable InconsistentNaming
    public class ElectionContractTestBase : ContractTestBase<ElectionContractTestModule>
    {
        protected IBlockTimeProvider BlockTimeProvider =>
            Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();

        protected Timestamp StartTimestamp => TimestampHelper.GetUtcNow();

        protected ECKeyPair BootMinerKeyPair => SampleECKeyPairs.KeyPairs[0];

        protected Address BootMinerAddress => Address.FromPublicKey(BootMinerKeyPair.PublicKey);

        internal static List<ECKeyPair> InitialMinersKeyPairs =>
            SampleECKeyPairs.KeyPairs.Take(InitialMinersCount).ToList();

        internal static List<ECKeyPair> FullNodesKeyPairs =>
            SampleECKeyPairs.KeyPairs.Skip(InitialMinersCount).Take(FullNodesCount).ToList();

        internal static List<ECKeyPair> VotersKeyPairs => SampleECKeyPairs.KeyPairs
            .Skip(InitialMinersCount + FullNodesCount).Take(VotersCount).ToList();

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
        protected Address TreasuryContractAddress { get; set; }

        protected Hash MinerElectionVotingItemId { get; set; }

        // Will use BootMinerKeyPair.
        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        internal VoteContractContainer.VoteContractStub VoteContractStub { get; set; }
        internal ProfitContractContainer.ProfitContractStub ProfitContractStub { get; set; }
        internal ElectionContractContainer.ElectionContractStub ElectionContractStub { get; set; }
        internal AEDPoSContractContainer.AEDPoSContractStub AEDPoSContractStub { get; set; }
        internal TreasuryContractContainer.TreasuryContractStub TreasuryContractStub { get; set; }
        

        private byte[] ConsensusContractCode => Codes.Single(kv => kv.Key.Contains("AEDPoS")).Value;
        private byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
        private byte[] ElectionContractCode => Codes.Single(kv => kv.Key.Contains("Election")).Value;
        private byte[] ProfitContractCode => Codes.Single(kv => kv.Key.Contains("Profit")).Value;
        private byte[] VoteContractCode => Codes.Single(kv => kv.Key.Contains("Vote")).Value;       
        private byte[] TreasuryContractCode => Codes.Single(kv => kv.Key.Contains("Treasury")).Value;

        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroStub(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractStub(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }

        internal VoteContractContainer.VoteContractStub GetVoteContractStub(ECKeyPair keyPair)
        {
            return GetTester<VoteContractContainer.VoteContractStub>(VoteContractAddress, keyPair);
        }

        internal ProfitContractContainer.ProfitContractStub GetProfitContractStub(ECKeyPair keyPair)
        {
            return GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, keyPair);
        }
        
        internal ElectionContractContainer.ElectionContractStub GetElectionContractStub(ECKeyPair keyPair)
        {
            return GetTester<ElectionContractContainer.ElectionContractStub>(ElectionContractAddress, keyPair);
        }
        
        internal TreasuryContractContainer.TreasuryContractStub GetTreasuryContractStub(ECKeyPair keyPair)
        {
            return GetTester<TreasuryContractContainer.TreasuryContractStub>(TreasuryContractAddress, keyPair);
        }

        internal AEDPoSContractContainer.AEDPoSContractStub GetAEDPoSContractStub(
            ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractContainer.AEDPoSContractStub>(ConsensusContractAddress, keyPair);
        }

        protected void InitializeContracts()
        {
            BasicContractZeroStub = GetContractZeroStub(BootMinerKeyPair);

            BlockTimeProvider.SetBlockTime(StartTimestamp);

            // Deploy Vote Contract
            VoteContractAddress = AsyncHelper.RunSync(  () =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    VoteContractCode,
                    VoteSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
            VoteContractStub = GetVoteContractStub(BootMinerKeyPair);
            AsyncHelper.RunSync(InitializeVote);
            
            // Deploy Profit Contract
            ProfitContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    ProfitContractCode,
                    ProfitSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
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
            ElectionContractStub = GetElectionContractStub(BootMinerKeyPair);
            AsyncHelper.RunSync(InitializeElection);
            
            // Deploy Treasury Contract.
            TreasuryContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                TreasuryContractCode,
                TreasurySmartContractAddressNameProvider.Name,
                BootMinerKeyPair));
            TreasuryContractStub = GetTreasuryContractStub(BootMinerKeyPair);
            AsyncHelper.RunSync(InitializeTreasury);
            
            // Deploy Token Contract
            TokenContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    TokenContractCode,
                    TokenSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
            TokenContractStub = GetTokenContractStub(BootMinerKeyPair);
            AsyncHelper.RunSync(InitializeToken);

            // Deploy AElf Consensus Contract.
            ConsensusContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                ConsensusContractCode,
                ConsensusSmartContractAddressNameProvider.Name,
                BootMinerKeyPair));

            AEDPoSContractStub = GetAEDPoSContractStub(BootMinerKeyPair);
            AsyncHelper.RunSync(InitializeAEDPoS);

            var profitIds = AsyncHelper.RunSync(() =>
                ProfitContractStub.GetCreatedProfitIds.CallAsync(
                    new GetCreatedProfitIdsInput
                    {
                        Creator = ElectionContractAddress
                    })).ProfitIds;
            ProfitItemsIds = new Dictionary<ProfitType, Hash>
            {
                {ProfitType.Treasury, profitIds[0]},
                {ProfitType.MinerReward, profitIds[1]},
                {ProfitType.BackupSubsidy, profitIds[2]},
                {ProfitType.CitizenWelfare, profitIds[3]},
                {ProfitType.BasicMinerReward, profitIds[4]},
                {ProfitType.VotesWeightReward, profitIds[5]},
                {ProfitType.ReElectionReward, profitIds[6]},
            };

            MinerElectionVotingItemId = AsyncHelper.RunSync(() =>
                ElectionContractStub.GetMinerElectionVotingItemId.CallAsync(new Empty()));
        }


        internal enum ProfitType
        {
            Treasury,
            MinerReward,
            BackupSubsidy,
            CitizenWelfare,
            BasicMinerReward,
            VotesWeightReward,
            ReElectionReward
        }
        
        private async Task InitializeVote()
        {
            var result = await VoteContractStub.InitialVoteContract.SendAsync(
                new Empty());
            CheckResult(result.TransactionResult);
        }

        private async Task InitializeElection()
        {
            var result = await ElectionContractStub.InitialElectionContract.SendAsync(new InitialElectionContractInput
                {
                    MaximumLockTime = 1080 * 86400,
                    MinimumLockTime = 90 * 86400
                });
            CheckResult(result.TransactionResult);
        }
        
        private async Task InitializeAEDPoS()
        {
            var result1 = await AEDPoSContractStub.InitialAElfConsensusContract.SendAsync(
                new InitialAElfConsensusContractInput
                {
                    TimeEachTerm = 604800L
                });
            CheckResult(result1.TransactionResult);
            var result2 = await AEDPoSContractStub.FirstRound.SendAsync(
                new MinerList
                    {
                        PublicKeys = {InitialMinersKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey))}
                    }.GenerateFirstRoundOfNewTerm(MiningInterval, StartTimestamp));
            CheckResult(result2.TransactionResult);
        }

        private async Task InitializeToken()
        {
            var result1 = await TokenContractStub.CreateNativeToken.SendAsync(new CreateNativeTokenInput
            {
                Symbol = ElectionContractTestConstants.NativeTokenSymbol,
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = ElectionContractTestConstants.NativeTokenTotalSupply,
                Issuer = BootMinerAddress,
                LockWhiteSystemContractNameList =
                {
                    ElectionSmartContractAddressNameProvider.Name,
                    VoteSmartContractAddressNameProvider.Name,
                    ProfitSmartContractAddressNameProvider.Name,
                }
            });
            CheckResult(result1.TransactionResult);

            for (var i = 0; i < InitialMinersCount + FullNodesCount + VotersCount; i++)
            {
                if (i < InitialMinersCount)
                {
                    var result3 = await TokenContractStub.Issue.SendAsync(new IssueInput
                    {
                        Symbol = ElectionContractTestConstants.NativeTokenSymbol,
                        Amount = ElectionContractConstants.LockTokenForElection * 10,
                        To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[i].PublicKey),
                        Memo = "Initial balance for initial miners."
                    });
                    CheckResult(result3.TransactionResult);
                    continue;
                }
                
                if (i < InitialMinersCount + FullNodesCount)
                {
                    var result3 = await TokenContractStub.Issue.SendAsync(new IssueInput
                    {
                        Symbol = ElectionContractTestConstants.NativeTokenSymbol,
                        Amount = ElectionContractConstants.LockTokenForElection * 10,
                        To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[i].PublicKey),
                        Memo = "Initial balance for initial full nodes."
                    });
                    CheckResult(result3.TransactionResult);
                    continue;
                }
                
                var result4 = await TokenContractStub.Issue.SendAsync(new IssueInput
                {
                    Symbol = ElectionContractTestConstants.NativeTokenSymbol,
                    Amount = ElectionContractConstants.LockTokenForElection - 1,
                    To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[i].PublicKey),
                    Memo = "Initial balance for voters."
                });
                CheckResult(result4.TransactionResult);
            }
        }

        private async Task InitializeProfit()
        {
            var result = await ProfitContractStub.InitializeProfitContract.SendAsync(new Empty());
            CheckResult(result.TransactionResult);
        }
        
        private async Task InitializeTreasury()
        {
            {
                var result =
                    await TreasuryContractStub.InitialTreasuryContract.SendAsync(new InitialTreasuryContractInput());
                CheckResult(result.TransactionResult);
            }
            {
                var result =
                    await TreasuryContractStub.InitialMiningRewardProfitItem.SendAsync(
                        new InitialMiningRewardProfitItemInput());
                CheckResult(result.TransactionResult);
            }
        }

        private void CheckResult(TransactionResult result)
        {
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new Exception(result.Error);
            }
        }

        internal async Task NextTerm(ECKeyPair keyPair)
        {
            var miner = GetAEDPoSContractStub(keyPair);
            var round = await miner.GetCurrentRoundInformation.CallAsync(new Empty());
            var victories = await ElectionContractStub.GetVictories.CallAsync(new Empty());
            var miners = new MinerList
            {
                PublicKeys =
                {
                    victories.Value
                }
            };
            var firstRoundOfNextTerm =
                miners.GenerateFirstRoundOfNewTerm(MiningInterval, BlockTimeProvider.GetBlockTime(), round.RoundNumber, round.TermNumber);
            var executionResult = (await miner.NextTerm.SendAsync(firstRoundOfNextTerm)).TransactionResult;
            executionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        internal async Task NextRound(ECKeyPair keyPair)
        {
            var miner = GetAEDPoSContractStub(keyPair);
            var round = await miner.GetCurrentRoundInformation.CallAsync(new Empty());
            round.GenerateNextRoundInformation(
                StartTimestamp.ToDateTime().AddMilliseconds(round.TotalMilliseconds()).ToTimestamp(), StartTimestamp,
                out var nextRound);
            await miner.NextRound.SendAsync(nextRound);
        }

        internal async Task NormalBlock(ECKeyPair keyPair)
        {
            var miner = GetAEDPoSContractStub(keyPair);
            var round = await miner.GetCurrentRoundInformation.CallAsync(new Empty());
            var minerInRound = round.RealTimeMinersInformation[keyPair.PublicKey.ToHex()];
            await miner.UpdateValue.SendAsync(new UpdateValueInput
            {
                OutValue = Hash.Generate(),
                Signature = Hash.Generate(),
                PreviousInValue = minerInRound.PreviousInValue ?? Hash.Empty,
                RoundId = round.RoundId,
                ProducedBlocks = minerInRound.ProducedBlocks + 1,
                ActualMiningTime = minerInRound.ExpectedMiningTime,
                SupposedOrderOfNextRound = 1
            });
        }

        internal async Task<long> GetNativeTokenBalance(byte[] publicKey)
        {
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = ElectionContractTestConstants.NativeTokenSymbol,
                Owner = Address.FromPublicKey(publicKey)
            })).Balance;

            return balance;
        }
        
        internal async Task<long> GetVoteTokenBalance(byte[] publicKey)
        {
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = ElectionContractConstants.VoteSymbol,
                Owner = Address.FromPublicKey(publicKey)
            })).Balance;

            return balance;
        }
    }
}