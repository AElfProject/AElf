using System.Numerics;
using Shouldly;
using Xunit;

namespace AElf.Types.Tests
{
    public class UInt256ValueTest
    {
        [Fact]
        public void UInt256ValueNormalTest()
        {
            var foo = new UInt256Value(ulong.MaxValue);
            var addOne = new UInt256Value(0, 1);
            (foo + new UInt256Value(1)).ToString().ShouldBe(addOne.ToString());

            var timesTwo = new UInt256Value(ulong.MaxValue - 1, 1);
            (foo * new UInt256Value(2)).ShouldBe(timesTwo);
        }
    }
}