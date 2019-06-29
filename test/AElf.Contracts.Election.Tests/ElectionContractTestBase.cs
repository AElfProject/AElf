using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Economic;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.Profit;
using AElf.Contracts.TestKit;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Token;
using AElf.Sdk.CSharp;
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
        protected Address EconomicContractAddress { get; set; }
        protected Address ParliamentAuthContractAddress { get; set; }
        
        protected Address TokenConverterContractAddress { get; set; }
        protected Hash MinerElectionVotingItemId { get; set; }

        // Will use BootMinerKeyPair.
        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        internal VoteContractContainer.VoteContractStub VoteContractStub { get; set; }
        internal ProfitContractContainer.ProfitContractStub ProfitContractStub { get; set; }
        internal ElectionContractContainer.ElectionContractStub ElectionContractStub { get; set; }
        internal AEDPoSContractContainer.AEDPoSContractStub AEDPoSContractStub { get; set; }
        internal TreasuryContractContainer.TreasuryContractStub TreasuryContractStub { get; set; }
        internal EconomicContractContainer.EconomicContractStub EconomicContractStub { get; set; }
        internal ParliamentAuthContractContainer.ParliamentAuthContractStub ParliamentAuthContractStub { get; set; }
        internal TokenConverterContractContainer.TokenConverterContractStub TokenConverterContractStub { get; set; }

        private byte[] ConsensusContractCode => Codes.Single(kv => kv.Key.Contains("AEDPoS")).Value;
        private byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
        private byte[] TokenConverterContractCode => Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value;
        private byte[] ElectionContractCode => Codes.Single(kv => kv.Key.Contains("Election")).Value;
        private byte[] ProfitContractCode => Codes.Single(kv => kv.Key.Contains("Profit")).Value;
        private byte[] VoteContractCode => Codes.Single(kv => kv.Key.Contains("Vote")).Value;       
        private byte[] TreasuryContractCode => Codes.Single(kv => kv.Key.Contains("Treasury")).Value;
        private byte[] EconomicContractCode => Codes.Single(kv => kv.Key.Contains("Economic")).Value;
        private byte[] ParliamentAuthContractCode => Codes.Single(kv => kv.Key.Contains("ParliamentAuth")).Value;

        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroStub(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractStub(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }
        
        internal TokenConverterContractContainer.TokenConverterContractStub GetTokenConverterContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterContractAddress,
                keyPair);
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
        internal EconomicContractContainer.EconomicContractStub GetEconomicContractStub(ECKeyPair keyPair)
        {
            return GetTester<EconomicContractContainer.EconomicContractStub>(EconomicContractAddress, keyPair);
        }
        internal ParliamentAuthContractContainer.ParliamentAuthContractStub GetParliamentAuthContractStub(
            ECKeyPair keyPair)
        {
            return GetTester<ParliamentAuthContractContainer.ParliamentAuthContractStub>(ParliamentAuthContractAddress,
                keyPair);
        }

        private void DeployContracts()
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
            
            // Deploy Profit Contract
            ProfitContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    ProfitContractCode,
                    ProfitSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
            ProfitContractStub =
                GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, BootMinerKeyPair);
            
            // Deploy Election Contract.
            ElectionContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    ElectionContractCode,
                    ElectionSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
            ElectionContractStub = GetElectionContractStub(BootMinerKeyPair);
            
            // Deploy Treasury Contract.
            TreasuryContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                TreasuryContractCode,
                TreasurySmartContractAddressNameProvider.Name,
                BootMinerKeyPair));
            TreasuryContractStub = GetTreasuryContractStub(BootMinerKeyPair);
            
            // Deploy Token Contract
            TokenContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    TokenContractCode,
                    TokenSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
            TokenContractStub = GetTokenContractStub(BootMinerKeyPair);
            
            // Deploy AElf Consensus Contract.
            ConsensusContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                ConsensusContractCode,
                ConsensusSmartContractAddressNameProvider.Name,
                BootMinerKeyPair));
            AEDPoSContractStub = GetAEDPoSContractStub(BootMinerKeyPair);
            
            // Deploy Token Converter Contract
            TokenConverterContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    TokenConverterContractCode,
                    TokenConverterSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
            TokenConverterContractStub = GetTokenConverterContractTester(BootMinerKeyPair);
            
            //Deploy economic Contract
            EconomicContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                EconomicContractCode,
                EconomicSmartContractAddressNameProvider.Name,
                BootMinerKeyPair));
            EconomicContractStub = GetEconomicContractStub(BootMinerKeyPair);
            
            //Deploy ParliamentAuth Contract
            ParliamentAuthContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    ParliamentAuthContractCode,
                    ParliamentAuthContractAddressNameProvider.Name,
                    BootMinerKeyPair));
            ParliamentAuthContractStub = GetParliamentAuthContractStub(BootMinerKeyPair);
        }
        
        protected void InitializeContracts()
        {
            DeployContracts();
            
            //AsyncHelper.RunSync(InitializeVote);
            AsyncHelper.RunSync(InitializeProfit);
            AsyncHelper.RunSync(InitializeTreasury);
            AsyncHelper.RunSync(InitializeElection);
            AsyncHelper.RunSync(InitializeParliamentContract);
            AsyncHelper.RunSync(InitializeEconomic);
            AsyncHelper.RunSync(InitializeToken);
            AsyncHelper.RunSync(InitializeAEDPoS);
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
        
//        private async Task InitializeVote()
//        {
//            var result = await VoteContractStub.InitialVoteContract.SendAsync(
//                new Empty());
//            CheckResult(result.TransactionResult);
//        }

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
//            var result1 = await TokenContractStub.Create.SendAsync(new CreateInput
//            {
//                Symbol = ElectionContractTestConstants.NativeTokenSymbol,
//                Decimals = 2,
//                IsBurnable = true,
//                TokenName = "elf token",
//                TotalSupply = ElectionContractTestConstants.NativeTokenTotalSupply,
//                Issuer = BootMinerAddress,
//                LockWhiteList =
//                {
//                    ElectionContractAddress,
//                    VoteContractAddress,
//                    ProfitContractAddress
//                }
//            });
//            CheckResult(result1.TransactionResult);

            for (var i = 0; i < InitialMinersCount + FullNodesCount + VotersCount; i++)
            {
                if (i < InitialMinersCount)
                {
                    var result3 = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
                    {
                        Amount = ElectionContractConstants.LockTokenForElection * 10,
                        To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[i].PublicKey),
                        Memo = "Initial balance for initial miners."
                    });
                    CheckResult(result3.TransactionResult);
                    continue;
                }
                
                if (i < InitialMinersCount + FullNodesCount)
                {
                    var result3 = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
                    {
                        Amount = ElectionContractConstants.LockTokenForElection * 10,
                        To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[i].PublicKey),
                        Memo = "Initial balance for initial full nodes."
                    });
                    CheckResult(result3.TransactionResult);
                    continue;
                }
                
                var result4 = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
                {
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
            
            //get profit id
            var profitIds = AsyncHelper.RunSync(() =>
                ProfitContractStub.GetCreatedProfitIds.CallAsync(
                    new GetCreatedProfitIdsInput
                    {
                        Creator = TreasuryContractAddress
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
        }

        private async Task InitializeEconomic()
        {
            //create native token
            {
                var result = await EconomicContractStub.InitialEconomicSystem.SendAsync(new InitialEconomicSystemInput
                {
                    NativeTokenDecimals = ElectionContractTestConstants.Decimals,
                    IsNativeTokenBurnable = ElectionContractTestConstants.IsBurnable,
                    NativeTokenSymbol = ElectionContractTestConstants.NativeTokenSymbol,
                    NativeTokenTotalSupply = ElectionContractTestConstants.NativeTokenTotalSupply,
                    MiningRewardTotalAmount = ElectionContractTestConstants.NativeTokenTotalSupply / 5
                });
                CheckResult(result.TransactionResult);
            }

            //Issue native token to core data center keyPairs
            {
                var result = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
                {
                    Amount = ElectionContractTestConstants.NativeTokenTotalSupply / 5,
                    To = TreasuryContractAddress,
                    Memo = "Set mining rewards."
                });
                CheckResult(result.TransactionResult);
            }
            
            MinerElectionVotingItemId = await ElectionContractStub.GetMinerElectionVotingItemId.CallAsync(new Empty());
        }
        
        private async Task InitializeParliamentContract()
        {
            var initializeResult = await ParliamentAuthContractStub.Initialize.SendAsync(new Empty());
            CheckResult(initializeResult.TransactionResult);
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
        
        private async Task SetConnector(Connector connector)
        {
            var connectorManagerAddress = await TokenConverterContractStub.GetManagerAddress.CallAsync(new Empty());
            var proposal = new CreateProposalInput
            {
                OrganizationAddress = connectorManagerAddress,
                ContractMethodName = nameof(TokenConverterContractStub.SetConnector),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = connector.ToByteString(),
                ToAddress = TokenConverterContractAddress
            };
            var createResult = await ParliamentAuthContractStub.CreateProposal.SendAsync(proposal);
            CheckResult(createResult.TransactionResult);

            var proposalHash = Hash.FromMessage(proposal);
            var approveResult = await ParliamentAuthContractStub.Approve.SendAsync(new Acs3.ApproveInput
            {
                ProposalId = proposalHash,
            });
            CheckResult(approveResult.TransactionResult);
        }
    }
}