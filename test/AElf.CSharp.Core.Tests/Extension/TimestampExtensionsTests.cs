using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.CSharp.Core.Extension
{
    public class TimestampExtensionsTests
    {
        [Fact]
        public void Milliseconds_Test()
        {
            var duration = new Duration {Seconds = long.MaxValue / 1000 + 1};
            duration.Milliseconds().ShouldBe(long.MaxValue);

            duration = new Duration {Seconds = 10, Nanos = 1000000};
            duration.Milliseconds().ShouldBe(10001);
        }
    }
}