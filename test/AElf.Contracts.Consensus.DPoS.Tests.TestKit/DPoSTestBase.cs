using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.Dividend;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Contracts.Consensus.DPoS
{
    public class DPoSTestBase : ContractTestBase<DPoSTestAElfModule>
    {
        protected const int MinersCount = 5;
        protected const int CandidatesCount = 10;
        protected const int VotersCount = 10;
        protected const int MiningInterval = 4000;

        protected IBlockTimeProvider BlockTimeProvider =>
            Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();

        protected IECKeyPairProvider ECKeyPairProvider =>
            Application.ServiceProvider.GetRequiredService<IECKeyPairProvider>();

        protected Address ConsensusContractAddress { get; set; }

        protected Address DividendContractAddress { get; set; }

        protected Address TokenContractAddress { get; set; }

        internal ConsensusContractContainer.ConsensusContractStub BootMiner =>
            GetTester<ConsensusContractContainer.ConsensusContractStub>(ConsensusContractAddress, BootMinerKeyPair);

        protected ECKeyPair BootMinerKeyPair => SampleECKeyPairs.KeyPairs.First();

        protected List<ECKeyPair> InitialMinersKeyPairs => SampleECKeyPairs.KeyPairs.Take(MinersCount).ToList();

        protected List<ECKeyPair> CandidatesKeyPairs =>
            SampleECKeyPairs.KeyPairs.Skip(MinersCount).Take(CandidatesCount).ToList();

        protected List<ECKeyPair> VotersKeyPairs =>
            SampleECKeyPairs.KeyPairs.Skip(MinersCount + CandidatesCount).Take(VotersCount).ToList();

        protected DateTime BlockchainStartTime => DateTime.Parse("2019-01-01 00:00:00.000").ToUniversalTime();

        protected void InitializeContracts()
        {
            ECKeyPairProvider.SetECKeyPair(BootMinerKeyPair);
            // Deploy useful contracts.
            ConsensusContractAddress = AsyncHelper.RunSync(async () => await DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                File.ReadAllBytes(typeof(ConsensusContractContainer.ConsensusContractStub).Assembly.Location),
                ConsensusSmartContractAddressNameProvider.Name,
                BootMinerKeyPair));

            AsyncHelper.RunSync(async () => { await InitializeConsensus(); });
            DividendContractAddress = AsyncHelper.RunSync(async () =>
                await DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    File.ReadAllBytes(typeof(DividendContractContainer.DividendContractStub).Assembly.Location),
                    DividendSmartContractAddressNameProvider.Name,
                    BootMinerKeyPair));
            AsyncHelper.RunSync(async () => { await InitializeDividend(); });
            TokenContractAddress = AsyncHelper.RunSync(
                async () =>
                    await DeploySystemSmartContract(
                        KernelConstants.CodeCoverageRunnerCategory,
                        File.ReadAllBytes(typeof(TokenContractContainer.TokenContractStub).Assembly.Location),
                        TokenSmartContractAddressNameProvider.Name,
                        BootMinerKeyPair));
            AsyncHelper.RunSync(async () => { await InitializeToken(); });
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
                await GetConsensusContractTester(candidatesKeyPair).AnnounceElection.SendAsync(new Alias
                {
                    Value = candidatesKeyPair.PublicKey.ToHex().Substring(0, 20)
                });
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
                    Amount = 10_0000
                });
            }
        }

        protected async Task BootMinerChangeRoundAsync(long nextRoundNumber = 2)
        {
            var currentRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());
            var expectedStartTime = BlockchainStartTime.GetRoundExpectedStartTime(
                currentRound.TotalMilliseconds(MiningInterval), nextRoundNumber);
            currentRound.GenerateNextRoundInformation(expectedStartTime, BlockchainStartTime.ToTimestamp(),
                out var nextRound);
            await BootMiner.NextRound.SendAsync(nextRound);
        }

        internal ConsensusContractContainer.ConsensusContractStub GetConsensusContractTester(ECKeyPair keyPair)
        {
            return GetTester<ConsensusContractContainer.ConsensusContractStub>(ConsensusContractAddress, keyPair);
        }

        internal DividendContractContainer.DividendContractStub GetDividendContractTester(ECKeyPair keyPair)
        {
            return GetTester<DividendContractContainer.DividendContractStub>(DividendContractAddress, keyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }

        private async Task InitializeConsensus(Acs4.DPoSStrategyInput input = null)
        {
            var consensus =
                GetTester<ConsensusContractContainer.ConsensusContractStub>(ConsensusContractAddress, BootMinerKeyPair);
            await consensus.InitialDPoSContract.SendAsync(new InitialDPoSContractInput
            {
                TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                DividendsContractSystemName = DividendSmartContractAddressNameProvider.Name,
                LockTokenForElection = 100_000
            });
            await consensus.InitialConsensus.SendAsync(
                InitialMinersKeyPairs.Select(m => m.PublicKey.ToHex()).ToList().ToMiners()
                    .GenerateFirstRoundOfNewTerm(MiningInterval, BlockchainStartTime));
            await consensus.ConfigStrategy.SendAsync(input ?? new Acs4.DPoSStrategyInput()
            {
                IsTimeSlotSkippable = true,
                IsBlockchainAgeSettable = true,
                IsVerbose = false
            });
        }

        private async Task InitializeDividend()
        {
            await GetTester<DividendContractContainer.DividendContractStub>(DividendContractAddress, BootMinerKeyPair)
                .InitializeDividendContract.SendAsync(
                    new InitialDividendContractInput
                    {
                        ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name,
                        TokenContractSystemName = TokenSmartContractAddressNameProvider.Name
                    });
        }

        private async Task InitializeToken()
        {
            const string symbol = "ELF";
            const long totalSupply = 10_0000_0000;
            var issuer = Address.FromPublicKey(BootMinerKeyPair.PublicKey);
            var token = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, BootMinerKeyPair);
            await token.CreateNativeToken.SendAsync(new CreateNativeTokenInput
            {
                Symbol = symbol,
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = totalSupply,
                // Set the contract zero address as the issuer temporarily.
                Issuer = issuer,
                LockWhiteSystemContractNameList = {ConsensusSmartContractAddressNameProvider.Name}
            });
            await token.IssueNativeToken.SendAsync(new IssueNativeTokenInput
            {
                Symbol = symbol,
                Amount = (long) (totalSupply * 0.2),
                ToSystemContractName = DividendSmartContractAddressNameProvider.Name,
                Memo = "Set dividends.",
            });


            foreach (var initialMinerKeyPair in InitialMinersKeyPairs)
            {
                await token.Issue.SendAsync(new IssueInput
                {
                    Symbol = symbol,
                    Amount = (long) (totalSupply * 0.2) / MinersCount,
                    To = Address.FromPublicKey(initialMinerKeyPair.PublicKey),
                    Memo = "Set initial miner's balance.",
                });
            }

            await token.SetFeePoolAddress.SendAsync(DividendSmartContractAddressNameProvider.Name);
        }
    }
}