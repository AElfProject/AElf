using System.Numerics;
using Shouldly;
using Xunit;

namespace AElf.Types.Tests
{
    public class UInt256StructTest
    {
        [Fact]
        public void UInt256StructNormalTest()
        {
            var foo = new UInt256Struct(ulong.MaxValue);
            var addOne = new UInt256Struct(0, 1);
            (foo + new UInt256Struct(1)).ToString().ShouldBe(addOne.ToString());

            var timesTwo = new UInt256Struct(ulong.MaxValue - 1, 1);
            (foo * new UInt256Struct(2)).ShouldBe(timesTwo);
        }
    }
}