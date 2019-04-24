using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AElfConsensus;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Contracts.Profit
{
    public class AElfConsensusContractTestBase : ContractTestBase<AElfConsensusContractTestAElfModule>
    {
        protected Hash TreasuryHash { get; set; }
        protected ECKeyPair StarterKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address Starter => Address.FromPublicKey(StarterKeyPair.PublicKey);
        protected Address AElfConsensusContractAddress { get; set; }
        protected Address ElectionContractAddress { get; set; }
        
        internal List<AElfConsensusContractContainer.AElfConsensusContractStub> Creators => CreatorMinerKeyPair
            .Select(p => GetTester<AElfConsensusContractContainer.AElfConsensusContractStub>(ElectionContractAddress, p)).ToList();

        internal List<AElfConsensusContractContainer.AElfConsensusContractStub> Normal => NormalMinerKeyPair
            .Select(p => GetTester<AElfConsensusContractContainer.AElfConsensusContractStub>(ElectionContractAddress, p)).ToList();
        
        protected List<ECKeyPair> CreatorMinerKeyPair => SampleECKeyPairs.KeyPairs.Skip(1).Take(4).ToList();
        
        protected List<ECKeyPair> NormalMinerKeyPair => SampleECKeyPairs.KeyPairs.Skip(5).Take(5).ToList();

        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }

        internal ElectionContractContainer.ElectionContractStub ElectionContractStub { get; set; }

        internal AElfConsensusContractContainer.AElfConsensusContractStub AElfConsensusContractStub { get; set; }

        protected void InitializeContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(StarterKeyPair);
            
            ElectionContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ElectionContract).Assembly.Location)),
                        Name = ProfitSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateElectionInitializationCallList()
                    })).Output;
            AElfConsensusContractStub = GetAElfConsensusContractTester(StarterKeyPair);

            //deploy token contract
            AElfConsensusContractAddress = AsyncHelper.RunSync(() => GetContractZeroTester(StarterKeyPair)
                .DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(AElfConsensusContract).Assembly.Location)),
                        Name = TokenSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateTokenInitializationCallList()
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

        private SystemTransactionMethodCallList GenerateTokenInitializationCallList()
        {
            var aelfConsensusMethodCallList = new SystemTransactionMethodCallList();
            return aelfConsensusMethodCallList;
        }
    }
}