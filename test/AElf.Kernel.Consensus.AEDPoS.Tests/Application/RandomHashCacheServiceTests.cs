using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS.Tests.Application
{
    public class RandomHashCacheServiceTests : AEDPoSTestBase
    {
        private readonly IRandomHashCacheService _randomHashCacheService;

        public RandomHashCacheServiceTests()
        {
            _randomHashCacheService = GetRequiredService<IRandomHashCacheService>();
        }

        [Fact]
        public void RandomHash_Test()
        {
            var blockHash = Hash.FromString("blockHash");
            var result = _randomHashCacheService.GetRandomHash(blockHash);
            result.ShouldBe(Hash.Empty);

            var randomHash = Hash.FromString("randomHash");
            _randomHashCacheService.SetRandomHash(blockHash, randomHash);
            
            var queryResult = _randomHashCacheService.GetRandomHash(blockHash);
            queryResult.ShouldBe(randomHash);
        }

        [Fact]
        public void GetLatestGeneratedBlockRandomHash_Test()
        {
            var blockHash = Hash.FromString("blockHash");
            const long blockHeight = 5L;
            _randomHashCacheService.SetGeneratedBlockPreviousBlockInformation(blockHash, blockHeight);
            var queryResult = _randomHashCacheService.GetLatestGeneratedBlockRandomHash();
            queryResult.ShouldBe(Hash.Empty);
        }

        [Fact]
        public void SetGeneratedBlockPreviousBlockInformation()
        {
            const long blockHeight = 5L;
            var randomHash1 = Hash.FromString("randomHash1");
            var previousHash = Hash.FromString("previousHash");
            
            var blockHash = Hash.FromString("blockHash");
            var randomHash2 = Hash.FromString("randomHash2");
            
            _randomHashCacheService.SetRandomHash(previousHash, randomHash1);
            _randomHashCacheService.SetGeneratedBlockPreviousBlockInformation(previousHash, blockHeight);
            
            _randomHashCacheService.SetRandomHash(blockHash, randomHash2);
            _randomHashCacheService.SetGeneratedBlockPreviousBlockInformation(blockHash, blockHeight + 1);
            
            var queryResult = _randomHashCacheService.GetLatestGeneratedBlockRandomHash();
            queryResult.ShouldBe(randomHash1);
        }
    }
}