using AElf.Kernel.Miner.Application;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Miner
{
    public sealed class SystemTransactionExtraDataProviderTests : AElfKernelTestBase
    {
        private readonly ISystemTransactionExtraDataProvider _systemTransactionExtraDataProvider;

        public SystemTransactionExtraDataProviderTests()
        {
            _systemTransactionExtraDataProvider = GetRequiredService<ISystemTransactionExtraDataProvider>();
        }
        
        [Fact]
        public void SetAndGetSystemTransactionCountTest()
        {
            var blockHeader = new BlockHeader();
            _systemTransactionExtraDataProvider.TryGetSystemTransactionCount(blockHeader,out var systemTransactionCount).ShouldBeFalse();
            systemTransactionCount.ShouldBe(0);

            var count = 10;
            _systemTransactionExtraDataProvider.SetSystemTransactionCount(count, blockHeader);

            _systemTransactionExtraDataProvider.TryGetSystemTransactionCount(blockHeader, out systemTransactionCount).ShouldBeTrue();
            systemTransactionCount.ShouldBe(count);
        }
    }
}