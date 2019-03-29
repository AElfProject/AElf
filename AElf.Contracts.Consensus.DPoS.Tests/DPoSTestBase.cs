using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

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

        protected Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();

        protected ConsensusContractContainer.ConsensusContractTester BootNode =>
            GetTester<ConsensusContractContainer.ConsensusContractTester>(ContractZeroAddress, BootNodeKeyPair);

        protected ECKeyPair BootNodeKeyPair => SampleECKeyPairs.KeyPairs.First();

        protected List<ECKeyPair> InitialMiners => SampleECKeyPairs.KeyPairs.Take(MinersCount).ToList();

        protected List<ECKeyPair> Candidates =>
            SampleECKeyPairs.KeyPairs.Skip(MinersCount).Take(CandidatesCount).ToList();

        protected List<ECKeyPair> Voters =>
            SampleECKeyPairs.KeyPairs.Skip(MinersCount + CandidatesCount).Take(VotersCount).ToList();

        protected async Task InitialConsensus()
        {
        }
    }
}