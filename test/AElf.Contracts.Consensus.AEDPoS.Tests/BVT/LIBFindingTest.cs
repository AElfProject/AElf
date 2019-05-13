using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography;
using Xunit;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public class LIBFindingTest : AElfConsensusContractTestBase
    {
        private AEDPoSContractContainer.AEDPoSContractStub Starter { get; }
        
        private List<AEDPoSContractContainer.AEDPoSContractStub> Miners { get; }

        private const int MinersCount = 17;

        public LIBFindingTest()
        {
            Starter = BootMiner;
            var minersKeyPairs = Enumerable.Range(0, MinersCount).Select(_ => CryptoHelpers.GenerateKeyPair()).ToList();
            Miners = Enumerable.Range(0, 17)
                .Select(i => GetAElfConsensusContractTester(minersKeyPairs[i])).ToList();
        }
        
        [Fact]
        public async Task GetLIBOffsetTest()
        {
        }
    }
}