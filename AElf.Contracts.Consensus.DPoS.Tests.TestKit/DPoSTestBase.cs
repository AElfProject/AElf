using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.Dividend;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Contracts.Consensus.DPoS
{
    public class DPoSTestBase : ContractTestBase<DPoSTestAElfModule>
    {
        public const int MinersCount = 5;
        public const int CandidatesCount = 10;
        public const int VotersCount = 10;
        public const int MiningInterval = 4000;

        protected ISmartContractAddressService ContractAddressService =>
            Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();

        protected IBlockTimeProvider BlockTimeProvider =>
            Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();
        
        protected IECKeyPairProvider ECKeyPairProvider =>
            Application.ServiceProvider.GetRequiredService<IECKeyPairProvider>();
        
        protected Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();

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

        protected DateTime BlockchainStartTime => DateTime.Parse("2019-04-01 00:00:00.000").ToUniversalTime();

        protected void InitializeContracts(DPoSStrategyInput input = null)
        {
            ECKeyPairProvider.SetECKeyPair(BootMinerKeyPair);
            // Deploy useful contracts.
            ConsensusContractAddress = AsyncHelper.RunSync(() => GetContractZeroTester(BootMinerKeyPair)
                .DeploySystemSmartContract.SendAsync(new SystemContractDeploymentInput
                {
                    Category = KernelConstants.DefaultRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ConsensusContract).Assembly.Location)),
                    Name = ConsensusSmartContractAddressNameProvider.Name,
                    TransactionMethodCallList = GenerateConsensusInitializationCallList(input)
                })).Output;

            DividendContractAddress = AsyncHelper.RunSync(() => GetContractZeroTester(BootMinerKeyPair)
                .DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.DefaultRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(DividendContract).Assembly.Location)),
                        Name = DividendsSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateDividendInitializationCallList()
                    })).Output;
            
            TokenContractAddress = AsyncHelper.RunSync(() => GetContractZeroTester(BootMinerKeyPair)
                .DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.DefaultRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location)),
                        Name = TokenSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateTokenInitializationCallList()
                    })).Output;
        }

        protected async Task ChangeRound(long nextRoundNumber = 2)
        {
            var currentRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());
            var expectedStartTime = BlockchainStartTime.GetRoundExpectedStartTime(
                currentRound.TotalMilliseconds(MiningInterval), nextRoundNumber);
            currentRound.GenerateNextRoundInformation(expectedStartTime, BlockchainStartTime.ToTimestamp(),
                out var nextRound);
            await BootMiner.NextRound.SendAsync(nextRound);
        }

        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }
        
        internal ConsensusContractContainer.ConsensusContractStub GetConsensusContractTester(ECKeyPair keyPair)
        {
            return GetTester<ConsensusContractContainer.ConsensusContractStub>(ConsensusContractAddress, keyPair);
        }
        
        internal DividendContractContainer.DividendContractStub GetDividendContractTester(ECKeyPair keyPair)
        {
            return GetTester<DividendContractContainer.DividendContractStub>(DividendContractAddress, keyPair);
        }

        private SystemTransactionMethodCallList GenerateConsensusInitializationCallList(DPoSStrategyInput input = null)
        {
            var consensusMethodCallList = new SystemTransactionMethodCallList();
            consensusMethodCallList.Add(nameof(ConsensusContract.InitialDPoSContract),
                new InitialDPoSContractInput
                {
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                    DividendsContractSystemName = DividendsSmartContractAddressNameProvider.Name
                });
            consensusMethodCallList.Add(nameof(ConsensusContract.InitialConsensus),
                InitialMinersKeyPairs.Select(m => m.PublicKey.ToHex()).ToList().ToMiners().GenerateFirstRoundOfNewTerm(
                    MiningInterval, BlockchainStartTime));
            consensusMethodCallList.Add(nameof(ConsensusContract.ConfigStrategy),
                input ?? new DPoSStrategyInput
                {
                    IsTimeSlotSkippable = true,
                    IsBlockchainAgeSettable = true,
                    IsVerbose = false
                });
            return consensusMethodCallList;
        }

        private SystemTransactionMethodCallList GenerateDividendInitializationCallList()
        {
            var dividendMethodCallList = new SystemTransactionMethodCallList();
            dividendMethodCallList.Add(nameof(DividendContract.InitializeDividendContract),
                new InitialDividendContractInput
                {
                    ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name,
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name
                });
            return dividendMethodCallList;
        }
        
        private SystemTransactionMethodCallList GenerateTokenInitializationCallList()
        {
            const string symbol = "ELF";
            const long totalSupply = 10_0000_0000;
            var issuer = Address.FromPublicKey(BootMinerKeyPair.PublicKey);
            var tokenContractCallList = new SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContract.CreateNativeToken), new CreateNativeTokenInput
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

            tokenContractCallList.Add(nameof(TokenContract.IssueNativeToken), new IssueNativeTokenInput
            {
                Symbol = symbol,
                Amount = (long)(totalSupply * 0.2),
                ToSystemContractName = DividendsSmartContractAddressNameProvider.Name,
                Memo = "Set dividends.",
            });

            foreach (var initialMinerKeyPair in InitialMinersKeyPairs)
            {
                tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
                {
                    Symbol = symbol,
                    Amount = (long) (totalSupply * 0.2) / MinersCount,
                    To = Address.FromPublicKey(initialMinerKeyPair.PublicKey),
                    Memo = "Set initial miner's balance.",
                });
            }

            // Set fee pool address to dividend contract address.
            tokenContractCallList.Add(nameof(TokenContract.SetFeePoolAddress),
                DividendsSmartContractAddressNameProvider.Name);

            return tokenContractCallList;
        }
    }
}