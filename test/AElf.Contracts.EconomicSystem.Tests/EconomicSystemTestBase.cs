using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Economic;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.TestContract.MethodCallThreshold;
using AElf.Contracts.TestContract.TransactionFeeCharging;
using AElf.Contracts.TestKit;
using AElf.Contracts.TokenConverter;
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

namespace AElf.Contracts.EconomicSystem.Tests
{
    // ReSharper disable InconsistentNaming
    public class EconomicSystemTestBase : ContractTestBase<EconomicSystemTestModule>
    {
        protected IBlockTimeProvider BlockTimeProvider =>
            Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();

        protected Timestamp StartTimestamp => TimestampHelper.GetUtcNow();

        protected ECKeyPair BootMinerKeyPair => SampleECKeyPairs.KeyPairs[0];

        protected Address BootMinerAddress => Address.FromPublicKey(BootMinerKeyPair.PublicKey);

        protected Address ConnectorManagerAddress => Address.FromPublicKey(BootMinerKeyPair.PublicKey);

        internal static List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
            SampleECKeyPairs.KeyPairs.Take(EconomicSystemTestConstants.InitialCoreDataCenterCount).ToList();

        internal static List<ECKeyPair> CoreDataCenterKeyPairs =>
            SampleECKeyPairs.KeyPairs.Skip(EconomicSystemTestConstants.InitialCoreDataCenterCount)
                .Take(EconomicSystemTestConstants.CoreDataCenterCount).ToList();

        internal static List<ECKeyPair> ValidationDataCenterKeyPairs =>
            SampleECKeyPairs.KeyPairs
                .Skip(EconomicSystemTestConstants.InitialCoreDataCenterCount +
                      EconomicSystemTestConstants.CoreDataCenterCount)
                .Take(EconomicSystemTestConstants.ValidateDataCenterCount).ToList();

        internal static List<ECKeyPair> ValidationDataCenterCandidateKeyPairs =>
            SampleECKeyPairs.KeyPairs
                .Skip(EconomicSystemTestConstants.InitialCoreDataCenterCount +
                      EconomicSystemTestConstants.CoreDataCenterCount +
                      EconomicSystemTestConstants.ValidateDataCenterCount)
                .Take(EconomicSystemTestConstants.ValidateDataCenterCandidateCount).ToList();

        internal static List<ECKeyPair> VoterKeyPairs =>
            SampleECKeyPairs.KeyPairs
                .Skip(EconomicSystemTestConstants.InitialCoreDataCenterCount +
                      EconomicSystemTestConstants.CoreDataCenterCount +
                      EconomicSystemTestConstants.ValidateDataCenterCount +
                      EconomicSystemTestConstants.ValidateDataCenterCandidateCount)
                .Take(EconomicSystemTestConstants.VoterCount).ToList();

        internal Dictionary<ProfitType, Hash> ProfitItemsIds { get; set; }

        protected Address TokenContractAddress { get; set; }
        protected Address VoteContractAddress { get; set; }
        protected Address ProfitContractAddress { get; set; }
        protected Address ElectionContractAddress { get; set; }
        protected Address ConsensusContractAddress { get; set; }
        protected Address TokenConverterContractAddress { get; set; }
        protected Address TreasuryContractAddress { get; set; }
        protected Address TransactionFeeChargingContractAddress { get; set; }
        protected Address MethodCallThresholdContractAddress { get; set; }
        protected Address EconomicContractAddress { get; set; }

        // Will use BootMinerKeyPair.
        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        internal TokenConverterContractContainer.TokenConverterContractStub TokenConverterContractStub { get; set; }
        internal VoteContractContainer.VoteContractStub VoteContractStub { get; set; }
        internal ProfitContractContainer.ProfitContractStub ProfitContractStub { get; set; }
        internal ElectionContractContainer.ElectionContractStub ElectionContractStub { get; set; }
        internal AEDPoSContractContainer.AEDPoSContractStub AEDPoSContractStub { get; set; }
        internal TreasuryContractContainer.TreasuryContractStub TreasuryContractStub { get; set; }

        internal TransactionFeeChargingContractContainer.TransactionFeeChargingContractStub
            TransactionFeeChargingContractStub { get; set; }

        internal MethodCallThresholdContractContainer.MethodCallThresholdContractStub MethodCallThresholdContractStub
        {
            get;
            set;
        }

        internal EconomicContractContainer.EconomicContractStub EconomicContractStub { get; set; }

        private byte[] ConsensusContractCode => Codes.Single(kv => kv.Key.Contains("AEDPoS")).Value;
        private byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
        private byte[] TokenConverterContractCode => Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value;
        private byte[] ElectionContractCode => Codes.Single(kv => kv.Key.Contains("Election")).Value;
        private byte[] ProfitContractCode => Codes.First(kv => kv.Key.Contains("Profit")).Value;
        private byte[] VoteContractCode => Codes.Single(kv => kv.Key.Contains("Vote")).Value;
        private byte[] TreasuryContractCode => Codes.Single(kv => kv.Key.Contains("Treasury")).Value;

        private byte[] MethodCallThresholdContractCode =>
            Codes.Single(kv => kv.Key.Contains("MethodCallThreshold")).Value;

        private byte[] EconomicContractCode => Codes.Single(kv => kv.Key.Contains("Economic")).Value;

        private byte[] TransactionFeeChargingContractCode =>
            Codes.Single(kv => kv.Key.Contains("TransactionFee")).Value;

        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }

        internal TokenConverterContractContainer.TokenConverterContractStub GetTokenConverterContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterContractAddress,
                keyPair);
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

        internal AEDPoSContractContainer.AEDPoSContractStub GetAEDPoSContractStub(ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractContainer.AEDPoSContractStub>(ConsensusContractAddress, keyPair);
        }

        internal TreasuryContractContainer.TreasuryContractStub GetTreasuryContractStub(ECKeyPair keyPair)
        {
            return GetTester<TreasuryContractContainer.TreasuryContractStub>(TreasuryContractAddress, keyPair);
        }

        internal TransactionFeeChargingContractContainer.TransactionFeeChargingContractStub
            GetTransactionFeeChargingContractStub(ECKeyPair keyPair)
        {
            return GetTester<TransactionFeeChargingContractContainer.TransactionFeeChargingContractStub>(
                TransactionFeeChargingContractAddress, keyPair);
        }

        internal MethodCallThresholdContractContainer.MethodCallThresholdContractStub
            GetMethodCallThresholdContractStub(
                ECKeyPair keyPair)
        {
            return GetTester<MethodCallThresholdContractContainer.MethodCallThresholdContractStub>(
                MethodCallThresholdContractAddress,
                keyPair);
        }

        internal EconomicContractContainer.EconomicContractStub GetEconomicContractStub(ECKeyPair keyPair)
        {
            return GetTester<EconomicContractContainer.EconomicContractStub>(EconomicContractAddress, keyPair);
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

        protected void InitializeContracts()
        {
            DeployContracts();

            AsyncHelper.RunSync(InitializeVote);
            AsyncHelper.RunSync(InitializeProfit);
            AsyncHelper.RunSync(InitializeTreasuryConverter);
            AsyncHelper.RunSync(InitializeElection);
            AsyncHelper.RunSync(InitializeEconomicContract);
            AsyncHelper.RunSync(InitializeToken);
            AsyncHelper.RunSync(InitializeAElfConsensus);
            AsyncHelper.RunSync(InitializeTokenConverter);
            AsyncHelper.RunSync(InitializeTransactionFeeChargingContract);
            AsyncHelper.RunSync(InitializeMethodCallThresholdContract);
        }

        #region Deploy and initialize contracts

        private void DeployContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(BootMinerKeyPair);

            BlockTimeProvider.SetBlockTime(StartTimestamp);

            // Deploy Vote Contract
            VoteContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    VoteContractCode,
                    VoteSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
            VoteContractStub = GetVoteContractTester(BootMinerKeyPair);

            // Deploy Profit Contract
            ProfitContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    ProfitContractCode,
                    ProfitSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
            ProfitContractStub =
                GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, BootMinerKeyPair);

            // Deploy Treasury Contract.
            TreasuryContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                TreasuryContractCode,
                TreasurySmartContractAddressNameProvider.Name,
                BootMinerKeyPair));
            TreasuryContractStub = GetTreasuryContractStub(BootMinerKeyPair);

            // Deploy Election Contract.
            ElectionContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    ElectionContractCode,
                    ElectionSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
            ElectionContractStub = GetElectionContractTester(BootMinerKeyPair);

            // Deploy Token Contract
            TokenContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    TokenContractCode,
                    TokenSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
            TokenContractStub = GetTokenContractTester(BootMinerKeyPair);

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

            // Deploy Economic Contract
            EconomicContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    EconomicContractCode,
                    EconomicSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
            EconomicContractStub = GetEconomicContractStub(BootMinerKeyPair);

            // Deploy Contracts for testing.
            TransactionFeeChargingContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                TransactionFeeChargingContractCode,
                Hash.FromString("AElf.ContractNames.TransactionFeeCharging"),
                BootMinerKeyPair));
            TransactionFeeChargingContractStub = GetTransactionFeeChargingContractStub(BootMinerKeyPair);

            MethodCallThresholdContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                MethodCallThresholdContractCode,
                Hash.FromString("AElf.ContractNames.MethodCallThreshold"),
                BootMinerKeyPair));
            MethodCallThresholdContractStub = GetMethodCallThresholdContractStub(BootMinerKeyPair);
        }

        private async Task InitializeVote()
        {
            var result = await VoteContractStub.InitialVoteContract.SendAsync(
                new Empty());
            CheckResult(result.TransactionResult);
        }

        private async Task InitializeProfit()
        {
            var result = await ProfitContractStub.InitializeProfitContract.SendAsync(new Empty());
            CheckResult(result.TransactionResult);
        }

        private async Task InitializeTreasuryConverter()
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
            //get profit ids
            {
                var profitIds = (await ProfitContractStub.GetCreatedProfitIds.CallAsync(
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

        private async Task InitializeToken()
        {
            //issue some to default user and buy resource
            {
                var issueResult = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
                {
                    Amount = 1000_000L,
                    To = Address.FromPublicKey(BootMinerKeyPair.PublicKey),
                    Memo = "Used to transfer other testers"
                });
                CheckResult(issueResult.TransactionResult);
            }

            foreach (var coreDataCenterKeyPair in CoreDataCenterKeyPairs)
            {
                var result = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
                {
                    Amount = ElectionContractConstants.LockTokenForElection * 10,
                    To = Address.FromPublicKey(coreDataCenterKeyPair.PublicKey),
                    Memo = "Used to announce election."
                });
                CheckResult(result.TransactionResult);
            }

            foreach (var validationDataCenterKeyPair in ValidationDataCenterKeyPairs)
            {
                var result = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
                {
                    Amount = ElectionContractConstants.LockTokenForElection,
                    To = Address.FromPublicKey(validationDataCenterKeyPair.PublicKey),
                    Memo = "Used to announce election."
                });
                CheckResult(result.TransactionResult);
            }

            foreach (var validationDataCenterCandidateKeyPair in ValidationDataCenterCandidateKeyPairs)
            {
                var result = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
                {
                    Amount = ElectionContractConstants.LockTokenForElection,
                    To = Address.FromPublicKey(validationDataCenterCandidateKeyPair.PublicKey),
                    Memo = "Used to announce election."
                });
                CheckResult(result.TransactionResult);
            }

            foreach (var voterKeyPair in VoterKeyPairs)
            {
                var result = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
                {
                    Amount = ElectionContractConstants.LockTokenForElection,
                    To = Address.FromPublicKey(voterKeyPair.PublicKey),
                    Memo = "Used to vote data center."
                });
                CheckResult(result.TransactionResult);
            }
        }

        private async Task InitializeAElfConsensus()
        {
            {
                var result = await AEDPoSContractStub.InitialAElfConsensusContract.SendAsync(
                    new InitialAElfConsensusContractInput
                    {
                        TimeEachTerm = 604800L
                    });
                CheckResult(result.TransactionResult);
            }
            {
                var result = await AEDPoSContractStub.FirstRound.SendAsync(
                    new MinerList
                    {
                        PublicKeys = {InitialCoreDataCenterKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey))}
                    }.GenerateFirstRoundOfNewTerm(EconomicSystemTestConstants.MiningInterval, StartTimestamp));
                CheckResult(result.TransactionResult);
            }
        }

        private async Task InitializeTransactionFeeChargingContract()
        {
            var result = await TransactionFeeChargingContractStub.InitializeTransactionFeeChargingContract.SendAsync(
                new InitializeTransactionFeeChargingContractInput
                {
                    Symbol = EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol
                });
            CheckResult(result.TransactionResult);
            
            var approveResult = await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol,
                Spender = TokenConverterContractAddress,
                Amount = 1000_000L
            });
            approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task InitializeMethodCallThresholdContract()
        {
            var result = await MethodCallThresholdContractStub.InitializeMethodCallThresholdContract.SendAsync(
                new InitializeMethodCallThresholdContractInput
                {
                    Symbol = EconomicSystemTestConstants.MethodCallThresholdContractTokenSymbol
                });
            CheckResult(result.TransactionResult);
        }

        private async Task InitializeTokenConverter()
        {
            {
                var result = await TokenConverterContractStub.Initialize.SendAsync(new InitializeInput
                {
                    BaseTokenSymbol = EconomicSystemTestConstants.NativeTokenSymbol,
                    ManagerAddress = ConnectorManagerAddress,
                    FeeRate = "0.01"
                });
                CheckResult(result.TransactionResult);
            }
            
            await SetConnectors();
        }

        private async Task SetConnectors()
        {
            //Transfer manager to BootMiner
            {
                var result = await TokenConverterContractStub.SetManagerAddress.SendAsync(BootMinerAddress);
                CheckResult(result.TransactionResult);
            }
            
            var manager =
                GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterContractAddress,
                    BootMinerKeyPair);
            {
                var result = await manager.SetConnector.SendAsync(new Connector
                {
                    Symbol = EconomicSystemTestConstants.NativeTokenSymbol,
                    IsPurchaseEnabled = true,
                    Weight = "0.5",
                    IsVirtualBalanceEnabled = true,
                    VirtualBalance = 1_000_000
                });
                CheckResult(result.TransactionResult);
            }
            {
                var result = await manager.SetConnector.SendAsync(new Connector
                {
                    Symbol = EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol,
                    IsPurchaseEnabled = true,
                    Weight = "0.2",
                    IsVirtualBalanceEnabled = true
                });
                CheckResult(result.TransactionResult);
            }
            {
                var result = await manager.SetConnector.SendAsync(new Connector
                {
                    Symbol = EconomicSystemTestConstants.MethodCallThresholdContractTokenSymbol,
                    IsPurchaseEnabled = true,
                    Weight = "0.2",
                    IsVirtualBalanceEnabled = true
                });
                CheckResult(result.TransactionResult);
            }

            //Transfer manager to TreasuryContractAddress
            {
                var result = await TokenConverterContractStub.SetManagerAddress.SendAsync(TreasuryContractAddress);
                CheckResult(result.TransactionResult);
            }
        }

        private async Task InitializeEconomicContract()
        {
            //create native token
            {
                var result = await EconomicContractStub.CreateNativeToken.SendAsync(new CreateNativeTokenInput
                {
                    Decimals = EconomicSystemTestConstants.Decimals,
                    IsBurnable = EconomicSystemTestConstants.IsBurnable,
                    TokenName = EconomicSystemTestConstants.NativeTokenSymbol,
                    TotalSupply = EconomicSystemTestConstants.TotalSupply
                });
                CheckResult(result.TransactionResult);
            }
            //InitialMiningReward
            {
                var result = await EconomicContractStub.InitialMiningReward.SendAsync(new Empty());
                CheckResult(result.TransactionResult);
            }
            //Issue native token to core data center keyPairs
            {
                var result = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
                {
                    Amount = EconomicSystemTestConstants.TotalSupply / 5,
                    To = TreasuryContractAddress,
                    Memo = "Set mining rewards."
                });
                CheckResult(result.TransactionResult);
            }
            //Register election voting event
            {
                var result = await EconomicContractStub.RegisterElectionVotingEvent.SendAsync(new Empty());
                CheckResult(result.TransactionResult);
            }
        }

        #endregion

        #region Other Contracts Action and View

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
                miners.GenerateFirstRoundOfNewTerm(EconomicSystemTestConstants.MiningInterval,
                    BlockTimeProvider.GetBlockTime(), round.RoundNumber, round.TermNumber);
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
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol,
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

        #endregion

        private void CheckResult(TransactionResult result)
        {
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new Exception(result.Error);
            }
        }
    }
}