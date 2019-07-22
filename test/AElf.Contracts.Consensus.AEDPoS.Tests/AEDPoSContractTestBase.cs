using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Token;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;
using IBlockTimeProvider = AElf.Contracts.TestKit.IBlockTimeProvider;

namespace AElf.Contracts.Consensus.AEDPoS
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class AEDPoSContractTestBase : ContractTestBase<AEDPoSContractTestAElfModule>
    {
        protected IBlockTimeProvider BlockTimeProvider =>
            Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();

        protected IAElfAsymmetricCipherKeyPairProvider KeyPairProvider =>
            Application.ServiceProvider.GetRequiredService<IAElfAsymmetricCipherKeyPairProvider>();
        
        protected ITriggerInformationProvider TriggerInformationProvider => 
            Application.ServiceProvider.GetRequiredService<ITriggerInformationProvider>();

        protected ECKeyPair BootMinerKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address BootMinerAddress => Address.FromPublicKey(BootMinerKeyPair.PublicKey);
        protected Address AElfConsensusContractAddress { get; set; }
        protected Address ElectionContractAddress { get; set; }
        protected Address TokenContractAddress { get; set; }
        protected Address ProfitContractAddress { get; set; }
        protected Address VoteContractAddress { get; set; }

        internal List<AEDPoSContractImplContainer.AEDPoSContractImplStub> InitialMiners => InitialMinersKeyPairs
            .Select(p =>
                GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(AElfConsensusContractAddress, p))
            .ToList();

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub BootMiner => InitialMiners[0];

        internal List<AEDPoSContractImplContainer.AEDPoSContractImplStub> BackupNodes => BackupNodesKeyPair
            .Select(p =>
                GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(AElfConsensusContractAddress, p))
            .ToList();

        protected List<ECKeyPair> InitialMinersKeyPairs =>
            SampleECKeyPairs.KeyPairs.Take(AEDPoSContractTestConstants.InitialMinersCount).ToList();

        protected List<ECKeyPair> CandidatesKeyPairs =>
            SampleECKeyPairs.KeyPairs.Skip(AEDPoSContractTestConstants.InitialMinersCount)
                .Take(AEDPoSContractTestConstants.CandidatesCount).ToList();

        protected List<ECKeyPair> VotersKeyPairs =>
            SampleECKeyPairs.KeyPairs
                .Skip(AEDPoSContractTestConstants.InitialMinersCount + AEDPoSContractTestConstants.CandidatesCount)
                .Take(AEDPoSContractTestConstants.VotersCount).ToList();

        protected List<ECKeyPair> BackupNodesKeyPair =>
            SampleECKeyPairs.KeyPairs.Skip(AEDPoSContractTestConstants.InitialMinersCount)
                .Take(AEDPoSContractTestConstants.InitialMinersCount).ToList();

        internal ElectionContractContainer.ElectionContractStub ElectionContractStub { get; set; }

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub AEDPoSContractStub { get; set; }

        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }

        internal VoteContractContainer.VoteContractStub VoteContractStub { get; set; }
        internal ProfitContractContainer.ProfitContractStub ProfitContractStub { get; set; }

        private byte[] ConsensusContractCode => Codes.Single(kv => kv.Key.Contains("AEDPoS")).Value;
        private byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
        private byte[] ElectionContractCode => Codes.Single(kv => kv.Key.Contains("Election")).Value;
        private byte[] ProfitContractCode => Codes.Single(kv => kv.Key.Contains("Profit")).Value;
        private byte[] VoteContractCode => Codes.Single(kv => kv.Key.Contains("Vote")).Value;
        protected Timestamp BlockchainStartTimestamp => new Timestamp {Seconds = 0};

        protected void InitializeContracts()
        {
            KeyPairProvider.SetKeyPair(BootMinerKeyPair);

            // Deploy Vote Contract
            VoteContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    VoteContractCode,
                    VoteSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
            VoteContractStub = GetVoteContractTester(BootMinerKeyPair);
            AsyncHelper.RunSync(InitializeVoteContract);

            // Deploy Profit Contract
            ProfitContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    ProfitContractCode,
                    ProfitSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
            ProfitContractStub =
                GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, BootMinerKeyPair);
            AsyncHelper.RunSync(InitializeProfitContract);

            // Deploy Election Contract.
            ElectionContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    ElectionContractCode,
                    ElectionSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
            ElectionContractStub = GetElectionContractTester(BootMinerKeyPair);
            AsyncHelper.RunSync(InitializeElectionContract);

            // Deploy Token Contract
            TokenContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    TokenContractCode,
                    TokenSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
            TokenContractStub = GetTokenContractTester(BootMinerKeyPair);
            AsyncHelper.RunSync(InitializeTokenContract);

            // Deploy AEDPoS Contract.
            AElfConsensusContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    ConsensusContractCode,
                    ConsensusSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
            AEDPoSContractStub = GetAEDPoSContractStub(BootMinerKeyPair);
            AsyncHelper.RunSync(InitializeAEDPoSContract);
        }

        internal ElectionContractContainer.ElectionContractStub GetElectionContractTester(ECKeyPair keyPair)
        {
            return GetTester<ElectionContractContainer.ElectionContractStub>(ElectionContractAddress, keyPair);
        }

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub GetAEDPoSContractStub(
            ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(AElfConsensusContractAddress,
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
            var expectedStartTime = BlockchainStartTimestamp.ToDateTime()
                .AddMilliseconds(
                    ((long) currentRound.TotalMilliseconds(AEDPoSContractTestConstants.MiningInterval)).Mul(
                        nextRoundNumber.Sub(1)));
            currentRound.GenerateNextRoundInformation(expectedStartTime.ToTimestamp(), BlockchainStartTimestamp,
                out var nextRound);
            await BootMiner.NextRound.SendAsync(nextRound);
        }

        protected DateTime GetRoundExpectedStartTime(DateTime blockchainStartTime, int roundTotalMilliseconds,
            long roundNumber)
        {
            return blockchainStartTime.AddMilliseconds(roundTotalMilliseconds * (roundNumber - 1));
        }

        protected async Task InitializeCandidates(int take = AEDPoSContractTestConstants.CandidatesCount)
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

        #region Contract Initialization

        private async Task InitializeVoteContract()
        {
            var result = await VoteContractStub.InitialVoteContract.SendAsync(new Empty());
            CheckResult(result.TransactionResult);
        }

        private async Task InitializeElectionContract()
        {
            var result = await ElectionContractStub.InitialElectionContract.SendAsync(new InitialElectionContractInput
            {
                MaximumLockTime = 1080 * 86400,
                MinimumLockTime = 90 * 86400
            });
            CheckResult(result.TransactionResult);
        }

        private async Task InitializeAEDPoSContract()
        {
            {
                var result = await AEDPoSContractStub.InitialAElfConsensusContract.SendAsync(
                    new InitialAElfConsensusContractInput
                    {
                        TimeEachTerm = AEDPoSContractTestConstants.TimeEachTerm,
                        MinerIncreaseInterval = AEDPoSContractTestConstants.MinerIncreaseInterval
                    });
                CheckResult(result.TransactionResult);
            }
            {
                var result = await AEDPoSContractStub.FirstRound.SendAsync(
                    new MinerList
                    {
                        Pubkeys = {InitialMinersKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey))}
                    }.GenerateFirstRoundOfNewTerm(AEDPoSContractTestConstants.MiningInterval, BlockchainStartTimestamp.ToDateTime()));
                CheckResult(result.TransactionResult);
            }
        }

        private async Task InitializeTokenContract()
        {
            {
                var result = await TokenContractStub.CreateNativeToken.SendAsync(new CreateNativeTokenInput
                {
                    Symbol = AEDPoSContractTestConstants.Symbol,
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "elf token",
                    TotalSupply = AEDPoSContractTestConstants.TotalSupply,
                    Issuer = BootMinerAddress,
                    LockWhiteSystemContractNameList =
                    {
                        ElectionSmartContractAddressNameProvider.Name,
                        VoteSmartContractAddressNameProvider.Name,
                        ProfitSmartContractAddressNameProvider.Name,
                    }
                });
                CheckResult(result.TransactionResult);
            }

            {
                var result = await TokenContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
                {
                    Symbol = AEDPoSContractTestConstants.Symbol,
                    Amount = AEDPoSContractTestConstants.TotalSupply.Div(5),
                    // Should be address of Treasury Contract.
                    ToSystemContractName = ElectionSmartContractAddressNameProvider.Name,
                    Memo = "Set total mining rewards."
                });
                CheckResult(result.TransactionResult);
            }

            {
                var result = await TokenContractStub.Issue.SendAsync(new IssueInput
                {
                    Symbol = AEDPoSContractTestConstants.Symbol,
                    Amount = AEDPoSContractTestConstants.TotalSupply.Mul(3).Div(5),
                    To = BootMinerAddress,
                    Memo = "Issue token to default user.",
                });
                CheckResult(result.TransactionResult);
            }

            {
                foreach (var initialMinerKeyPair in InitialMinersKeyPairs)
                {
                    var result = await TokenContractStub.Issue.SendAsync(new IssueInput
                    {
                        Symbol = AEDPoSContractTestConstants.Symbol,
                        Amount = AEDPoSContractTestConstants.TotalSupply.Div(5)
                            .Div(AEDPoSContractTestConstants.InitialMinersCount),
                        To = Address.FromPublicKey(initialMinerKeyPair.PublicKey),
                        Memo = "Set initial miner's balance for testing."
                    });
                    CheckResult(result.TransactionResult);
                }
            }
        }

        private async Task InitializeProfitContract()
        {
            var result = await ProfitContractStub.InitializeProfitContract.SendAsync(new Empty());
            CheckResult(result.TransactionResult);
        }

        private void CheckResult(TransactionResult result)
        {
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new Exception(result.Error);
            }
        }

        #endregion
    }
}