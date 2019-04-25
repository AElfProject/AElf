using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Consensus.AElfConsensus;
using AElf.Contracts.Consensus.AElfConsensus;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Contracts.Consensus.AElfConsensus
{
    public class AElfConsensusContractTestBase : ContractTestBase<AElfConsensusContractTestAElfModule>
    {
        protected const int InitialMinersCount = 5;

        protected const int DaysEachTerm = 7;
        
        protected const int MiningInterval = 4000;
        
        protected ECKeyPair StarterKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address BootMinerAddress => Address.FromPublicKey(StarterKeyPair.PublicKey);
        protected Address AElfConsensusContractAddress { get; set; }
        protected Address ElectionContractAddress { get; set; }
        
        internal List<AElfConsensusContractContainer.AElfConsensusContractStub> InitialMiners => InitialMinersKeyPair
            .Select(p => GetTester<AElfConsensusContractContainer.AElfConsensusContractStub>(ElectionContractAddress, p)).ToList();

        internal AElfConsensusContractContainer.AElfConsensusContractStub BootMiner => InitialMiners[0];
        internal List<AElfConsensusContractContainer.AElfConsensusContractStub> BackupNodes => BackupNodesKeyPair
            .Select(p => GetTester<AElfConsensusContractContainer.AElfConsensusContractStub>(ElectionContractAddress, p)).ToList();
        
        protected List<ECKeyPair> InitialMinersKeyPair => SampleECKeyPairs.KeyPairs.Take(InitialMinersCount).ToList();
        
        protected List<ECKeyPair> BackupNodesKeyPair => SampleECKeyPairs.KeyPairs.Skip(InitialMinersCount).Take(InitialMinersCount).ToList();

        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }

        internal ElectionContractContainer.ElectionContractStub ElectionContractStub { get; set; }

        internal AElfConsensusContractContainer.AElfConsensusContractStub AElfConsensusContractStub { get; set; }

        protected void InitializeContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(StarterKeyPair);
            
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
            AElfConsensusContractStub = GetAElfConsensusContractTester(StarterKeyPair);

            // Deploy AElf Consensus Contract.
            AElfConsensusContractAddress = AsyncHelper.RunSync(() => GetContractZeroTester(StarterKeyPair)
                .DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(AElfConsensusContract).Assembly.Location)),
                        Name = ConsensusSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateAElfConsensusInitializationCallList()
                    })).Output;
            ElectionContractStub = GetElectionContractTester(StarterKeyPair);
        }

        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }

        internal ElectionContractContainer.ElectionContractStub GetElectionContractTester(ECKeyPair keyPair)
        {
            return GetTester<ElectionContractContainer.ElectionContractStub>(ElectionContractAddress, keyPair);
        }

        internal AElfConsensusContractContainer.AElfConsensusContractStub GetAElfConsensusContractTester(ECKeyPair keyPair)
        {
            return GetTester<AElfConsensusContractContainer.AElfConsensusContractStub>(AElfConsensusContractAddress, keyPair);
        }

        private SystemTransactionMethodCallList GenerateElectionInitializationCallList()
        {
            var electionMethodCallList = new SystemTransactionMethodCallList();
            return electionMethodCallList;
        }

        private SystemTransactionMethodCallList GenerateAElfConsensusInitializationCallList()
        {
            var aelfConsensusMethodCallList = new SystemTransactionMethodCallList();
            aelfConsensusMethodCallList.Add(nameof(AElfConsensusContract.InitialAElfConsensusContract), new InitialAElfConsensusContractInput
            {
                ElectionContractSystemName = ElectionSmartContractAddressNameProvider.Name,
                DaysEachTerm = DaysEachTerm
            });
            
            return aelfConsensusMethodCallList;
        }
    }
}