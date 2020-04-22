using System.Linq;
using Shouldly;
using Xunit;

namespace AElf.Types.Tests.Helper
{
    public class BlockTest
    {
        [Fact]
        public void Block_Test()
        {
            var hash1 = HashHelper.ComputeFromString("hash1");
            var bsPrefix = BlockHelper.GetRefBlockPrefix(hash1);
            bsPrefix.ShouldBe(hash1.Value.Take(4));
        }
    }
}