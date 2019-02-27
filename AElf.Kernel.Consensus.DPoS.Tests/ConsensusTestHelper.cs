using System.Collections.Generic;
using System.Linq;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;

namespace AElf.Kernel.Consensus.DPoS.Tests
{
    public static class ConsensusTestHelper
    {
        public static List<ECKeyPair> GenerateMinersKeyPairs(int minersCount)
        {
            var initialMinersKeyPairs = new List<ECKeyPair>();
            for (var i = 0; i < minersCount; i++)
            {
                initialMinersKeyPairs.Add(CryptoHelpers.GenerateKeyPair());
            }

            return initialMinersKeyPairs;
        }

        public static ConsensusTester CreateABootMiner()
        {
            var initialMinersKeyPairs = GenerateMinersKeyPairs(17);
            return new ConsensusTester(0, initialMinersKeyPairs[0], initialMinersKeyPairs, true);
        }

        public static TestMiners CreateTestMiners(int minersCount)
        {
            var minersKeyPairs = GenerateMinersKeyPairs(minersCount);

            return new TestMiners
            {
                BootMiner = new ConsensusTester(0, minersKeyPairs[0], minersKeyPairs, true),
                NormalMiners = new List<ConsensusTester>(Enumerable.Range(1, minersCount - 1)
                    .Select(x => new ConsensusTester(0, minersKeyPairs[x], minersKeyPairs)))
            };
        }
    }

    public class TestMiners
    {
        public ConsensusTester BootMiner { get; set; }
        public List<ConsensusTester> NormalMiners { get; set; }
    }
}