using System.Linq;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS.Tests
{
    public class ConsensusTestHelperTest
    {
        [Fact]
        public void CreateTestMinersTest()
        {
            var testMiners = ConsensusTestHelper.CreateTestMiners(17);

            Assert.Equal(16, testMiners.NormalMiners.Count);
            Assert.DoesNotContain(testMiners.BootMiner.CallOwnerKeyPair.PublicKey,
                testMiners.NormalMiners.Select(m => m.CallOwnerKeyPair.PublicKey));
        }

        [Fact]
        public void CreateABootMinerTest()
        {
            var bootMiner = ConsensusTestHelper.CreateABootMiner();
        }
        
    }
}