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
            var randomHash1 = Hash.FromString("hash1");
            var previousHashOfGeneratingBlock1 = Hash.FromString("hash2");
            var previousHeightOfGeneratingBlock1 = 100L;

            // Round 1:
            {
                // When generating block extra data. First time setting a random hash.
                RandomHashCacheService.SetGeneratedBlockPreviousBlockInformation(previousHashOfGeneratingBlock1,
                    previousHeightOfGeneratingBlock1);
                RandomHashCacheService.SetRandomHash(previousHashOfGeneratingBlock1, randomHash1);

                // Try to get previous random hash to generate trigger information for block extra data.
                {
                    var randomHash = RandomHashCacheService.GetLatestGeneratedBlockRandomHash();
                    randomHash.ShouldBe(Hash.Empty);
                }

                // When generating consensus transactions, get the same random hash of generating block extra data.
                {
                    var randomHash = RandomHashCacheService.GetRandomHash(previousHashOfGeneratingBlock1);
                    randomHash.ShouldBe(randomHash1);
                }

                // Try to get previous random hash.
                {
                    var randomHash = RandomHashCacheService.GetLatestGeneratedBlockRandomHash();
                    randomHash.ShouldBe(Hash.Empty);
                }
            }

            var randomHash2 = Hash.FromString("hash3");
            var previousHashOfGeneratingBlock2 = Hash.FromString("hash4");
            var previousHeightOfGeneratingBlock2 = 200L;

            // Round 2:
            {
                // Set a random hash when generating block extra data.
                RandomHashCacheService.SetGeneratedBlockPreviousBlockInformation(previousHashOfGeneratingBlock2,
                    previousHeightOfGeneratingBlock2);
                RandomHashCacheService.SetRandomHash(previousHashOfGeneratingBlock2, randomHash2);

                // Try to get previous random hash to generate trigger information for block extra data.
                {
                    var randomHash = RandomHashCacheService.GetLatestGeneratedBlockRandomHash();
                    randomHash.ShouldBe(randomHash1);
                }

                // When generating consensus transactions, get the same random hash of generating block extra data.
                {
                    var randomHash = RandomHashCacheService.GetRandomHash(previousHashOfGeneratingBlock2);
                    randomHash.ShouldBe(randomHash2);
                }

                // Try to get previous random hash.
                {
                    var randomHash = RandomHashCacheService.GetLatestGeneratedBlockRandomHash();
                    randomHash.ShouldBe(randomHash1);
                }
            }

            var randomHash3 = Hash.FromString("hash5");
            var previousHashOfGeneratingBlock3 = Hash.FromString("hash6");
            var previousHeightOfGeneratingBlock3 = 300L;

            // Round 3:
            {
                // Set a random hash when generating block extra data.
                RandomHashCacheService.SetGeneratedBlockPreviousBlockInformation(previousHashOfGeneratingBlock3,
                    previousHeightOfGeneratingBlock3);
                RandomHashCacheService.SetRandomHash(previousHashOfGeneratingBlock3, randomHash3);

                // Try to get previous random hash to generate trigger information for block extra data.
                {
                    var randomHash = RandomHashCacheService.GetLatestGeneratedBlockRandomHash();
                    randomHash.ShouldBe(randomHash2);
                }

                // When generating consensus transactions, get the same random hash of generating block extra data.
                {
                    var randomHash = RandomHashCacheService.GetRandomHash(previousHashOfGeneratingBlock3);
                    randomHash.ShouldBe(randomHash3);
                }

                // Try to get previous random hash.
                {
                    var randomHash = RandomHashCacheService.GetLatestGeneratedBlockRandomHash();
                    randomHash.ShouldBe(randomHash2);
                }
            }
            
            var randomHash4 = Hash.FromString("hash7");
            var previousHashOfGeneratingBlock4 = Hash.FromString("hash8");
            var previousHeightOfGeneratingBlock4 = 400L;

            // Round 3:
            {
                // Set a random hash when generating block extra data.
                RandomHashCacheService.SetGeneratedBlockPreviousBlockInformation(previousHashOfGeneratingBlock4,
                    previousHeightOfGeneratingBlock4);
                RandomHashCacheService.SetRandomHash(previousHashOfGeneratingBlock4, randomHash4);

                // Try to get previous random hash to generate trigger information for block extra data.
                {
                    var randomHash = RandomHashCacheService.GetLatestGeneratedBlockRandomHash();
                    randomHash.ShouldBe(randomHash3);
                }

                // When generating consensus transactions, get the same random hash of generating block extra data.
                {
                    var randomHash = RandomHashCacheService.GetRandomHash(previousHashOfGeneratingBlock4);
                    randomHash.ShouldBe(randomHash4);
                }

                // Try to get previous random hash.
                {
                    var randomHash = RandomHashCacheService.GetLatestGeneratedBlockRandomHash();
                    randomHash.ShouldBe(randomHash3);
                }
            }
        }
    }
}