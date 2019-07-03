using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Economic;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.Profit;
using AElf.Contracts.TestContract.MethodCallThreshold;
using AElf.Contracts.TestContract.TransactionFeeCharging;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;
using AElf.Cryptography.ECDSA;
using Volo.Abp.Threading;

namespace AElf.Contracts.EconomicSystem.Tests
{
    // ReSharper disable InconsistentNaming
//    public class EconomicSystemTestBase : ContractTestBase<EconomicSystemTestModule>
//    {
//        protected IBlockTimeProvider BlockTimeProvider =>
//            Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();
//
//        protected Timestamp StartTimestamp => TimestampHelper.GetUtcNow();
//
//        protected ECKeyPair BootMinerKeyPair => SampleECKeyPairs.KeyPairs[0];
//
//        protected Address BootMinerAddress => Address.FromPublicKey(BootMinerKeyPair.PublicKey);
//
//        protected Address ConnectorManagerAddress => Address.FromPublicKey(BootMinerKeyPair.PublicKey);
//
//        internal static List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
//            SampleECKeyPairs.KeyPairs.Take(EconomicSystemTestConstants.InitialCoreDataCenterCount).ToList();
//
//        internal static List<ECKeyPair> CoreDataCenterKeyPairs =>
//            SampleECKeyPairs.KeyPairs.Skip(EconomicSystemTestConstants.InitialCoreDataCenterCount)
//                .Take(EconomicSystemTestConstants.CoreDataCenterCount).ToList();
//
//        internal static List<ECKeyPair> ValidationDataCenterKeyPairs =>
//            SampleECKeyPairs.KeyPairs
//                .Skip(EconomicSystemTestConstants.InitialCoreDataCenterCount +
//                      EconomicSystemTestConstants.CoreDataCenterCount)
//                .Take(EconomicSystemTestConstants.ValidateDataCenterCount).ToList();
//
//        internal static List<ECKeyPair> ValidationDataCenterCandidateKeyPairs =>
//            SampleECKeyPairs.KeyPairs
//                .Skip(EconomicSystemTestConstants.InitialCoreDataCenterCount +
//                      EconomicSystemTestConstants.CoreDataCenterCount +
//                      EconomicSystemTestConstants.ValidateDataCenterCount)
//                .Take(EconomicSystemTestConstants.ValidateDataCenterCandidateCount).ToList();
//
//        internal static List<ECKeyPair> VoterKeyPairs =>
//            SampleECKeyPairs.KeyPairs
//                .Skip(EconomicSystemTestConstants.InitialCoreDataCenterCount +
//                      EconomicSystemTestConstants.CoreDataCenterCount +
//                      EconomicSystemTestConstants.ValidateDataCenterCount +
//                      EconomicSystemTestConstants.ValidateDataCenterCandidateCount)
//                .Take(EconomicSystemTestConstants.VoterCount).ToList();
//
//        internal Dictionary<ProfitType, Hash> ProfitItemsIds { get; set; }
//
//        protected Address TokenContractAddress { get; set; }
//        protected Address VoteContractAddress { get; set; }
//        protected Address ProfitContractAddress { get; set; }
//        protected Address ElectionContractAddress { get; set; }
//        protected Address ConsensusContractAddress { get; set; }
//        protected Address TokenConverterContractAddress { get; set; }
//        protected Address TreasuryContractAddress { get; set; }
//        protected Address TransactionFeeChargingContractAddress { get; set; }
//        protected Address MethodCallThresholdContractAddress { get; set; }
//        protected Address EconomicContractAddress { get; set; }
//        protected Address ParliamentAuthContractAddress { get; set; }
//
//        // Will use BootMinerKeyPair.
//        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }
//        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
//        internal TokenConverterContractContainer.TokenConverterContractStub TokenConverterContractStub { get; set; }
//        internal VoteContractContainer.VoteContractStub VoteContractStub { get; set; }
//        internal ProfitContractContainer.ProfitContractStub ProfitContractStub { get; set; }
//        internal ElectionContractContainer.ElectionContractStub ElectionContractStub { get; set; }
//        internal AEDPoSContractContainer.AEDPoSContractStub AEDPoSContractStub { get; set; }
//        internal TreasuryContractContainer.TreasuryContractStub TreasuryContractStub { get; set; }
//        internal ParliamentAuthContractContainer.ParliamentAuthContractStub ParliamentAuthContractStub { get; set; }
//
//        internal TransactionFeeChargingContractContainer.TransactionFeeChargingContractStub
//            TransactionFeeChargingContractStub { get; set; }
//
//        internal MethodCallThresholdContractContainer.MethodCallThresholdContractStub MethodCallThresholdContractStub
//        {
//            get;
//            set;
//        }
//
//        internal EconomicContractContainer.EconomicContractStub EconomicContractStub { get; set; }
//
//        private byte[] ConsensusContractCode => Codes.Single(kv => kv.Key.Contains("AEDPoS")).Value;
//        private byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
//        private byte[] TokenConverterContractCode => Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value;
//        private byte[] ElectionContractCode => Codes.Single(kv => kv.Key.Contains("Election")).Value;
//        private byte[] ProfitContractCode => Codes.First(kv => kv.Key.Contains("Profit")).Value;
//        private byte[] VoteContractCode => Codes.Single(kv => kv.Key.Contains("Vote")).Value;
//        private byte[] TreasuryContractCode => Codes.Single(kv => kv.Key.Contains("Treasury")).Value;
//        private byte[] ParliamentAuthContractCode => Codes.Single(kv => kv.Key.Contains("ParliamentAuth")).Value;
//
//        private byte[] MethodCallThresholdContractCode =>
//            Codes.Single(kv => kv.Key.Contains("MethodCallThreshold")).Value;
//
//        private byte[] EconomicContractCode => Codes.Single(kv => kv.Key.Contains("Economic")).Value;
//
//        private byte[] TransactionFeeChargingContractCode =>
//            Codes.Single(kv => kv.Key.Contains("TransactionFee")).Value;
//
//        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
//        {
//            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
//        }
//
//        internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
//        {
//            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
//        }
//
//        internal TokenConverterContractContainer.TokenConverterContractStub GetTokenConverterContractTester(
//            ECKeyPair keyPair)
//        {
//            return GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterContractAddress,
//                keyPair);
//        }
//
//        internal VoteContractContainer.VoteContractStub GetVoteContractTester(ECKeyPair keyPair)
//        {
//            return GetTester<VoteContractContainer.VoteContractStub>(VoteContractAddress, keyPair);
//        }
//
//        internal ProfitContractContainer.ProfitContractStub GetProfitContractTester(ECKeyPair keyPair)
//        {
//            return GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, keyPair);
//        }
//
//        internal ElectionContractContainer.ElectionContractStub GetElectionContractTester(ECKeyPair keyPair)
//        {
//            return GetTester<ElectionContractContainer.ElectionContractStub>(ElectionContractAddress, keyPair);
//        }
//
//        internal AEDPoSContractContainer.AEDPoSContractStub GetAEDPoSContractStub(ECKeyPair keyPair)
//        {
//            return GetTester<AEDPoSContractContainer.AEDPoSContractStub>(ConsensusContractAddress, keyPair);
//        }
//
//        internal TreasuryContractContainer.TreasuryContractStub GetTreasuryContractStub(ECKeyPair keyPair)
//        {
//            return GetTester<TreasuryContractContainer.TreasuryContractStub>(TreasuryContractAddress, keyPair);
//        }
//
//        internal ParliamentAuthContractContainer.ParliamentAuthContractStub GetParliamentAuthContractStub(
//            ECKeyPair keyPair)
//        {
//            return GetTester<ParliamentAuthContractContainer.ParliamentAuthContractStub>(ParliamentAuthContractAddress,
//                keyPair);
//        }
//
//        internal TransactionFeeChargingContractContainer.TransactionFeeChargingContractStub
//            GetTransactionFeeChargingContractStub(ECKeyPair keyPair)
//        {
//            return GetTester<TransactionFeeChargingContractContainer.TransactionFeeChargingContractStub>(
//                TransactionFeeChargingContractAddress, keyPair);
//        }
//
//        internal MethodCallThresholdContractContainer.MethodCallThresholdContractStub
//            GetMethodCallThresholdContractStub(
//                ECKeyPair keyPair)
//        {
//            return GetTester<MethodCallThresholdContractContainer.MethodCallThresholdContractStub>(
//                MethodCallThresholdContractAddress,
//                keyPair);
//        }
//
//        internal EconomicContractContainer.EconomicContractStub GetEconomicContractStub(ECKeyPair keyPair)
//        {
//            return GetTester<EconomicContractContainer.EconomicContractStub>(EconomicContractAddress, keyPair);
//        }
//
//        internal enum ProfitType
//        {
//            Treasury,
//            MinerReward,
//            BackupSubsidy,
//            CitizenWelfare,
//            BasicMinerReward,
//            VotesWeightReward,
//            ReElectionReward
//        }
//
//        protected void InitializeContracts()
//        {
//            DeployContracts();
//
//            AsyncHelper.RunSync(InitializeTreasuryConverter);
//            AsyncHelper.RunSync(InitializeElection);
//            AsyncHelper.RunSync(InitializeParliamentContract);
//            AsyncHelper.RunSync(InitializeEconomicContract);
//            AsyncHelper.RunSync(InitializeToken);
//            AsyncHelper.RunSync(InitializeAElfConsensus);
//            AsyncHelper.RunSync(InitializeTokenConverter);
//            AsyncHelper.RunSync(InitializeTransactionFeeChargingContract);
//        }
//
//        #region Deploy and initialize contracts
//
//        private void DeployContracts()
//        {
//            BasicContractZeroStub = GetContractZeroTester(BootMinerKeyPair);
//
//            BlockTimeProvider.SetBlockTime(StartTimestamp);
//
//            // Deploy Vote Contract
//            VoteContractAddress = AsyncHelper.RunSync(() =>
//                DeploySystemSmartContract(
//                    KernelConstants.CodeCoverageRunnerCategory,
//                    VoteContractCode,
//                    VoteSmartContractAddressNameProvider.Name,
//                    BootMinerKeyPair));
//            VoteContractStub = GetVoteContractTester(BootMinerKeyPair);
//
//            // Deploy Profit Contract
//            ProfitContractAddress = AsyncHelper.RunSync(() =>
//                DeploySystemSmartContract(
//                    KernelConstants.CodeCoverageRunnerCategory,
//                    ProfitContractCode,
//                    ProfitSmartContractAddressNameProvider.Name,
//                    BootMinerKeyPair));
//            ProfitContractStub =
//                GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, BootMinerKeyPair);
//
//            // Deploy Treasury Contract.
//            TreasuryContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(
//                KernelConstants.CodeCoverageRunnerCategory,
//                TreasuryContractCode,
//                TreasurySmartContractAddressNameProvider.Name,
//                BootMinerKeyPair));
//            TreasuryContractStub = GetTreasuryContractStub(BootMinerKeyPair);
//
//            // Deploy Election Contract.
//            ElectionContractAddress = AsyncHelper.RunSync(() =>
//                DeploySystemSmartContract(
//                    KernelConstants.CodeCoverageRunnerCategory,
//                    ElectionContractCode,
//                    ElectionSmartContractAddressNameProvider.Name,
//                    BootMinerKeyPair));
//            ElectionContractStub = GetElectionContractTester(BootMinerKeyPair);
//
//            // Deploy Token Contract
//            TokenContractAddress = AsyncHelper.RunSync(() =>
//                DeploySystemSmartContract(
//                    KernelConstants.CodeCoverageRunnerCategory,
//                    TokenContractCode,
//                    TokenSmartContractAddressNameProvider.Name,
//                    BootMinerKeyPair));
//            TokenContractStub = GetTokenContractTester(BootMinerKeyPair);
//
//            // Deploy AElf Consensus Contract.
//            ConsensusContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(
//                KernelConstants.CodeCoverageRunnerCategory,
//                ConsensusContractCode,
//                ConsensusSmartContractAddressNameProvider.Name,
//                BootMinerKeyPair));
//            AEDPoSContractStub = GetAEDPoSContractStub(BootMinerKeyPair);
//
//            // Deploy Token Converter Contract
//            TokenConverterContractAddress = AsyncHelper.RunSync(() =>
//                DeploySystemSmartContract(
//                    KernelConstants.CodeCoverageRunnerCategory,
//                    TokenConverterContractCode,
//                    TokenConverterSmartContractAddressNameProvider.Name,
//                    BootMinerKeyPair));
//            TokenConverterContractStub = GetTokenConverterContractTester(BootMinerKeyPair);
//
//            // Deploy Economic Contract
//            EconomicContractAddress = AsyncHelper.RunSync(() =>
//                DeploySystemSmartContract(
//                    KernelConstants.CodeCoverageRunnerCategory,
//                    EconomicContractCode,
//                    EconomicSmartContractAddressNameProvider.Name,
//                    BootMinerKeyPair));
//            EconomicContractStub = GetEconomicContractStub(BootMinerKeyPair);
//
//            //Deploy ParliamentAuth Contract
//            ParliamentAuthContractAddress = AsyncHelper.RunSync(() =>
//                DeploySystemSmartContract(
//                    KernelConstants.CodeCoverageRunnerCategory,
//                    ParliamentAuthContractCode,
//                    ParliamentAuthContractAddressNameProvider.Name,
//                    BootMinerKeyPair));
//            ParliamentAuthContractStub = GetParliamentAuthContractStub(BootMinerKeyPair);
//
//            // Deploy Contracts for testing.
//            TransactionFeeChargingContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(
//                KernelConstants.CodeCoverageRunnerCategory,
//                TransactionFeeChargingContractCode,
//                Hash.FromString("AElf.ContractNames.TransactionFeeCharging"),
//                BootMinerKeyPair));
//            TransactionFeeChargingContractStub = GetTransactionFeeChargingContractStub(BootMinerKeyPair);
//
//            MethodCallThresholdContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(
//                KernelConstants.CodeCoverageRunnerCategory,
//                MethodCallThresholdContractCode,
//                Hash.FromString("AElf.ContractNames.MethodCallThreshold"),
//                BootMinerKeyPair));
//            MethodCallThresholdContractStub = GetMethodCallThresholdContractStub(BootMinerKeyPair);
//        }
//
//        private async Task InitializeTreasuryConverter()
//        {
//            {
//                var result =
//                    await TreasuryContractStub.InitialTreasuryContract.SendAsync(new InitialTreasuryContractInput());
//                CheckResult(result.TransactionResult);
//            }
//            {
//                var result =
//                    await TreasuryContractStub.InitialMiningRewardProfitItem.SendAsync(
//                        new InitialMiningRewardProfitItemInput());
//                CheckResult(result.TransactionResult);
//            }
//            //get profit ids
//            {
//                var profitIds = (await ProfitContractStub.GetCreatedProfitIds.CallAsync(
//                    new GetCreatedProfitIdsInput
//                    {
//                        Creator = TreasuryContractAddress
//                    })).ProfitIds;
//                ProfitItemsIds = new Dictionary<ProfitType, Hash>
//                {
//                    {ProfitType.Treasury, profitIds[0]},
//                    {ProfitType.MinerReward, profitIds[1]},
//                    {ProfitType.BackupSubsidy, profitIds[2]},
//                    {ProfitType.CitizenWelfare, profitIds[3]},
//                    {ProfitType.BasicMinerReward, profitIds[4]},
//                    {ProfitType.VotesWeightReward, profitIds[5]},
//                    {ProfitType.ReElectionReward, profitIds[6]},
//                };
//            }
//        }
//
//        private async Task InitializeElection()
//        {
//            var result = await ElectionContractStub.InitialElectionContract.SendAsync(new InitialElectionContractInput
//            {
//                MaximumLockTime = 1080 * 86400,
//                MinimumLockTime = 90 * 86400
//            });
//            CheckResult(result.TransactionResult);
//        }
//
//        private async Task InitializeEconomicContract()
//        {
//            //create native token
//            {
//                var result = await EconomicContractStub.InitialEconomicSystem.SendAsync(new InitialEconomicSystemInput
//                {
//                    NativeTokenDecimals = EconomicSystemTestConstants.Decimals,
//                    IsNativeTokenBurnable = EconomicSystemTestConstants.IsBurnable,
//                    NativeTokenSymbol = EconomicSystemTestConstants.NativeTokenSymbol,
//                    NativeTokenTotalSupply = EconomicSystemTestConstants.TotalSupply,
//                    MiningRewardTotalAmount = EconomicSystemTestConstants.TotalSupply / 5
//                });
//                CheckResult(result.TransactionResult);
//            }
//
//            //Issue native token to core data center keyPairs
//            {
//                var result = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
//                {
//                    Amount = EconomicSystemTestConstants.TotalSupply / 5,
//                    To = TreasuryContractAddress,
//                    Memo = "Set mining rewards."
//                });
//                CheckResult(result.TransactionResult);
//            }
//        }
//
//        private async Task InitializeToken()
//        {
//            //issue some to default user and buy resource
//            {
//                var issueResult = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
//                {
//                    Amount = 1000_000_00000000L,
//                    To = Address.FromPublicKey(BootMinerKeyPair.PublicKey),
//                    Memo = "Used to transfer other testers"
//                });
//                CheckResult(issueResult.TransactionResult);
//            }
//
//            foreach (var coreDataCenterKeyPair in CoreDataCenterKeyPairs)
//            {
//                var result = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
//                {
//                    Amount = ElectionContractConstants.LockTokenForElection * 10,
//                    To = Address.FromPublicKey(coreDataCenterKeyPair.PublicKey),
//                    Memo = "Used to announce election."
//                });
//                CheckResult(result.TransactionResult);
//            }
//
//            foreach (var validationDataCenterKeyPair in ValidationDataCenterKeyPairs)
//            {
//                var result = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
//                {
//                    Amount = ElectionContractConstants.LockTokenForElection,
//                    To = Address.FromPublicKey(validationDataCenterKeyPair.PublicKey),
//                    Memo = "Used to announce election."
//                });
//                CheckResult(result.TransactionResult);
//            }
//
//            foreach (var validationDataCenterCandidateKeyPair in ValidationDataCenterCandidateKeyPairs)
//            {
//                var result = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
//                {
//                    Amount = ElectionContractConstants.LockTokenForElection,
//                    To = Address.FromPublicKey(validationDataCenterCandidateKeyPair.PublicKey),
//                    Memo = "Used to announce election."
//                });
//                CheckResult(result.TransactionResult);
//            }
//
//            foreach (var voterKeyPair in VoterKeyPairs)
//            {
//                var result = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
//                {
//                    Amount = ElectionContractConstants.LockTokenForElection,
//                    To = Address.FromPublicKey(voterKeyPair.PublicKey),
//                    Memo = "Used to vote data center."
//                });
//                CheckResult(result.TransactionResult);
//            }
//        }
//
//        private async Task InitializeAElfConsensus()
//        {
//            {
//                var result = await AEDPoSContractStub.InitialAElfConsensusContract.SendAsync(
//                    new InitialAElfConsensusContractInput
//                    {
//                        TimeEachTerm = 604800L
//                    });
//                CheckResult(result.TransactionResult);
//            }
//            {
//                var result = await AEDPoSContractStub.FirstRound.SendAsync(
//                    new MinerList
//                    {
//                        Pubkeys = {InitialCoreDataCenterKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey))}
//                    }.GenerateFirstRoundOfNewTerm(EconomicSystemTestConstants.MiningInterval, StartTimestamp));
//                CheckResult(result.TransactionResult);
//            }
//        }
//
//        private async Task InitializeTransactionFeeChargingContract()
//        {
//            var result = await TransactionFeeChargingContractStub.InitializeTransactionFeeChargingContract.SendAsync(
//                new InitializeTransactionFeeChargingContractInput
//                {
//                    Symbol = EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol
//                });
//            CheckResult(result.TransactionResult);
//
//            {
//                var approveResult = await TokenContractStub.Approve.SendAsync(new ApproveInput
//                {
//                    Symbol = EconomicSystemTestConstants.NativeTokenSymbol,
//                    Spender = TokenConverterContractAddress,
//                    Amount = 1000_000_00000000L
//                });
//                approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
//            }
//
//            foreach (var coreDataCenterKeyPair in CoreDataCenterKeyPairs)
//            {
//                var tokenStub = GetTokenContractTester(coreDataCenterKeyPair);
//                var approveResult = await tokenStub.Approve.SendAsync(new ApproveInput
//                {
//                    Symbol = EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol,
//                    Spender = TreasuryContractAddress,
//                    Amount = 1000_000_00000000L
//                });
//                approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
//            }
//        }
//
//        private async Task InitializeTokenConverter()
//        {
//            await SetConnectors();
//        }
//
//        private async Task InitializeParliamentContract()
//        {
//            var initializeResult = await ParliamentAuthContractStub.Initialize.SendAsync(new Empty());
//            CheckResult(initializeResult.TransactionResult);
//        }
//
//        private async Task SetConnectors()
//        {
//            {
//                await SetConnector(new Connector
//                {
//                    Symbol = EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol,
//                    IsPurchaseEnabled = true,
//                    Weight = "0.2",
//                    IsVirtualBalanceEnabled = true
//                });
//            }
//        }
//
//        #endregion
//
//        #region Other Contracts Action and View
//
//        internal async Task NextTerm(ECKeyPair keyPair)
//        {
//            var miner = GetAEDPoSContractStub(keyPair);
//            var round = await miner.GetCurrentRoundInformation.CallAsync(new Empty());
//            var victories = await ElectionContractStub.GetVictories.CallAsync(new Empty());
//            var miners = new MinerList
//            {
//                Pubkeys =
//                {
//                    victories.Value
//                }
//            };
//            var firstRoundOfNextTerm =
//                miners.GenerateFirstRoundOfNewTerm(EconomicSystemTestConstants.MiningInterval,
//                    BlockTimeProvider.GetBlockTime(), round.RoundNumber, round.TermNumber);
//            var executionResult = (await miner.NextTerm.SendAsync(firstRoundOfNextTerm)).TransactionResult;
//            executionResult.Status.ShouldBe(TransactionResultStatus.Mined);
//        }
//
//        internal async Task NextRound(ECKeyPair keyPair)
//        {
//            var miner = GetAEDPoSContractStub(keyPair);
//            var round = await miner.GetCurrentRoundInformation.CallAsync(new Empty());
//            round.GenerateNextRoundInformation(
//                StartTimestamp.ToDateTime().AddMilliseconds(round.TotalMilliseconds()).ToTimestamp(), StartTimestamp,
//                out var nextRound);
//            await miner.NextRound.SendAsync(nextRound);
//        }
//
//        internal async Task NormalBlock(ECKeyPair keyPair)
//        {
//            var miner = GetAEDPoSContractStub(keyPair);
//            var round = await miner.GetCurrentRoundInformation.CallAsync(new Empty());
//            var minerInRound = round.RealTimeMinersInformation[keyPair.PublicKey.ToHex()];
//            await miner.UpdateValue.SendAsync(new UpdateValueInput
//            {
//                OutValue = Hash.Generate(),
//                Signature = Hash.Generate(),
//                PreviousInValue = minerInRound.PreviousInValue ?? Hash.Empty,
//                RoundId = round.RoundId,
//                ProducedBlocks = minerInRound.ProducedBlocks + 1,
//                ActualMiningTime = minerInRound.ExpectedMiningTime,
//                SupposedOrderOfNextRound = 1
//            });
//        }
//
//        internal async Task<long> GetNativeTokenBalance(byte[] publicKey)
//        {
//            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
//            {
//                Symbol = EconomicSystemTestConstants.NativeTokenSymbol,
//                Owner = Address.FromPublicKey(publicKey)
//            })).Balance;
//
//            return balance;
//        }
//
//        internal async Task<long> GetVoteTokenBalance(byte[] publicKey)
//        {
//            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
//            {
//                Symbol = ElectionContractConstants.VoteSymbol,
//                Owner = Address.FromPublicKey(publicKey)
//            })).Balance;
//
//            return balance;
//        }
//
//        #endregion
//
//        private void CheckResult(TransactionResult result)
//        {
//            if (!string.IsNullOrEmpty(result.Error))
//            {
//                throw new Exception(result.Error);
//            }
//        }
//
//        private async Task SetConnector(Connector connector)
//        {
//            var connectorManagerAddress = await TokenConverterContractStub.GetManagerAddress.CallAsync(new Empty());
//            var proposal = new CreateProposalInput
//            {
//                OrganizationAddress = connectorManagerAddress,
//                ContractMethodName = nameof(TokenConverterContractStub.SetConnector),
//                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
//                Params = connector.ToByteString(),
//                ToAddress = TokenConverterContractAddress
//            };
//            var createResult = await ParliamentAuthContractStub.CreateProposal.SendAsync(proposal);
//            CheckResult(createResult.TransactionResult);
//
//            var proposalHash = Hash.FromMessage(proposal);
//            var approveResult = await ParliamentAuthContractStub.Approve.SendAsync(new Acs3.ApproveInput
//            {
//                ProposalId = proposalHash,
//            });
//            CheckResult(approveResult.TransactionResult);
//        }
//    }
    public class EconomicSystemTestBase : EconomicContractsTestBase
    {
        protected void InitializeContracts()
        {
            DeployAllContracts();
            
            AsyncHelper.RunSync(InitializeTreasuryConverter);
            AsyncHelper.RunSync(InitializeElection);
            AsyncHelper.RunSync(InitializeParliamentContract);
            AsyncHelper.RunSync(InitializeEconomicContract);
            AsyncHelper.RunSync(InitializeToken);
            AsyncHelper.RunSync(InitializeAElfConsensus);
            AsyncHelper.RunSync(InitializeTokenConverter);
            AsyncHelper.RunSync(InitializeTransactionFeeChargingContract);
        }
        
        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub =>
            GetBasicContractTester(BootMinerKeyPair);

        internal TokenContractContainer.TokenContractStub TokenContractStub => GetTokenContractTester(BootMinerKeyPair);

        internal TokenConverterContractContainer.TokenConverterContractStub TokenConverterContractStub =>
            GetTokenConverterContractTester(BootMinerKeyPair);

        internal VoteContractContainer.VoteContractStub VoteContractStub => GetVoteContractTester(BootMinerKeyPair);

        internal ProfitContractContainer.ProfitContractStub ProfitContractStub =>
            GetProfitContractTester(BootMinerKeyPair);

        internal ElectionContractContainer.ElectionContractStub ElectionContractStub =>
            GetElectionContractTester(BootMinerKeyPair);

        internal AEDPoSContractContainer.AEDPoSContractStub AEDPoSContractStub =>
            GetAEDPoSContractTester(BootMinerKeyPair);

        internal TreasuryContractContainer.TreasuryContractStub TreasuryContractStub =>
            GetTreasuryContractTester(BootMinerKeyPair);

        internal ParliamentAuthContractContainer.ParliamentAuthContractStub ParliamentAuthContractStub =>
            GetParliamentAuthContractTester(BootMinerKeyPair);

        internal TransactionFeeChargingContractContainer.TransactionFeeChargingContractStub
            TransactionFeeChargingContractStub => GetTransactionFeeChargingContractTester(BootMinerKeyPair);

        internal MethodCallThresholdContractContainer.MethodCallThresholdContractStub MethodCallThresholdContractStub =>
            GetMethodCallThresholdContractTester(BootMinerKeyPair);

        internal EconomicContractContainer.EconomicContractStub EconomicContractStub =>
            GetEconomicContractTester(BootMinerKeyPair);

        internal BasicContractZeroContainer.BasicContractZeroStub GetBasicContractTester(ECKeyPair keyPair)
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

        internal AEDPoSContractContainer.AEDPoSContractStub GetAEDPoSContractTester(ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractContainer.AEDPoSContractStub>(ConsensusContractAddress, keyPair);
        }

        internal TreasuryContractContainer.TreasuryContractStub GetTreasuryContractTester(ECKeyPair keyPair)
        {
            return GetTester<TreasuryContractContainer.TreasuryContractStub>(TreasuryContractAddress, keyPair);
        }

        internal ParliamentAuthContractContainer.ParliamentAuthContractStub GetParliamentAuthContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<ParliamentAuthContractContainer.ParliamentAuthContractStub>(ParliamentAuthContractAddress,
                keyPair);
        }

        internal TransactionFeeChargingContractContainer.TransactionFeeChargingContractStub
            GetTransactionFeeChargingContractTester(ECKeyPair keyPair)
        {
            return GetTester<TransactionFeeChargingContractContainer.TransactionFeeChargingContractStub>(
                TransactionFeeChargingContractAddress, keyPair);
        }

        internal MethodCallThresholdContractContainer.MethodCallThresholdContractStub
            GetMethodCallThresholdContractTester(
                ECKeyPair keyPair)
        {
            return GetTester<MethodCallThresholdContractContainer.MethodCallThresholdContractStub>(
                MethodCallThresholdContractAddress,
                keyPair);
        }

        internal EconomicContractContainer.EconomicContractStub GetEconomicContractTester(ECKeyPair keyPair)
        {
            return GetTester<EconomicContractContainer.EconomicContractStub>(EconomicContractAddress, keyPair);
        }
    }
}