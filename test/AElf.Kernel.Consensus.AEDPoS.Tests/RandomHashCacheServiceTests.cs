using System.Threading.Tasks;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS.Tests
{
    public class RandomHashCacheServiceTests : AEDPoSTestBase
    {
        [Fact]
        public async Task RandomHashCacheService_NormalProcess()
        {
            var randomHash1 = Hash.Generate();
            var previousHashOfGeneratingBlock1 = Hash.Generate();
            var previousHeightOfGeneratingBlock1 = 100L;

            // When generating block extra data. First time setting a random hash.
            RandomHashCacheService.SetGeneratedBlockPreviousBlockInformation(previousHashOfGeneratingBlock1,
                previousHeightOfGeneratingBlock1);
            RandomHashCacheService.SetRandomHash(previousHashOfGeneratingBlock1, randomHash1);
            
            // Try to get previous random hash to generate trigger information for block extra data.
            {
                var randomHash = RandomHashCacheService.GetLatestGeneratedBlockRandomHash();
                randomHash.ShouldBe(Hash.Empty);
            }
            
            
        }
    }
}