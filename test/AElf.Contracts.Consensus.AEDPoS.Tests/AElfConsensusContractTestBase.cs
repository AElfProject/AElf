using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.TestKit;
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

        protected const int MiningInterval = 4000;

        protected static readonly Timestamp StartTimestamp = DateTime.UtcNow.ToTimestamp();

        protected ECKeyPair BootMinerKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address BootMinerAddress => Address.FromPublicKey(BootMinerKeyPair.PublicKey);
        protected Address AElfConsensusContractAddress { get; set; }
        protected Address ElectionContractAddress { get; set; }

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

        protected List<ECKeyPair> BackupNodesKeyPair =>
            SampleECKeyPairs.KeyPairs.Skip(InitialMinersCount).Take(InitialMinersCount).ToList();

        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }

        internal ElectionContractContainer.ElectionContractStub ElectionContractStub { get; set; }

        internal AEDPoSContractContainer.AEDPoSContractStub AElfConsensusContractStub { get; set; }

        protected void InitializeContracts()
        {
            ECKeyPairProvider.SetKeyPair(BootMinerKeyPair);

            BasicContractZeroStub = GetContractZeroTester(BootMinerKeyPair);

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

//            AsyncHelper.RunSync(() =>
//                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
//                    new SystemContractDeploymentInput
//                    {
//                        Category = KernelConstants.CodeCoverageRunnerCategory,
//                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(MinersCountProviderContract).Assembly.Location)),
//                        Name = MinersCountProviderSmartContractAddress.Name,
//                        TransactionMethodCallList = GenerateElectionInitializationCallList()
//                    }));

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

        protected async Task BootMinerChangeRoundAsync(long nextRoundNumber = 2)
        {
            var currentRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());
            var expectedStartTime = StartTimestamp.ToDateTime()
                .AddMilliseconds(((long) currentRound.TotalMilliseconds(MiningInterval)).Mul(nextRoundNumber.Sub(1)));
            currentRound.GenerateNextRoundInformation(expectedStartTime, StartTimestamp, out var nextRound);
            await BootMiner.NextRound.SendAsync(nextRound);
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateElectionInitializationCallList()
        {
            var electionMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            electionMethodCallList.Add(nameof(ElectionContract.InitialElectionContract),
                new InitialElectionContractInput
                {
                    ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name
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
                    IsTermStayOne = true
                });
            aelfConsensusMethodCallList.Add(nameof(AEDPoSContract.FirstRound),
                new Miners
                    {
                        PublicKeys = {InitialMinersKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey))}
                    }.GenerateFirstRoundOfNewTerm(MiningInterval, StartTimestamp.ToDateTime()));
            return aelfConsensusMethodCallList;
        }
    }
}