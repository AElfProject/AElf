using Shouldly;
using Xunit;

namespace AElf.Types.Tests.Helper
{
    public class BlockHelperTest
    {
        [Fact]
        public void GetRefBlockPrefix_Test()
        {
            var blockHash = HashHelper.ComputeFrom("test");
            var get = BlockHelper.GetRefBlockPrefix(blockHash);
            get.Length.ShouldBe(4);
        }
    }
}