using Shouldly;
using Xunit;

namespace AElf.Kernel.Types.Tests
{
    public class TimestampHelperTest
    {
        [Fact]
        public void DurationFromMilliseconds_Test()
        { 
            var result= TimestampHelper.DurationFromMilliseconds(1000);
            result.Seconds.ShouldBe(1);
        }
        [Fact]
        public void DurationFromSeconds_Test()
        { 
            var result= TimestampHelper.DurationFromSeconds(1000);
            result.Seconds.ShouldBe(1000);
        }
        [Fact]
        public void DurationFromMinutes_Test()
        { 
            var result= TimestampHelper.DurationFromMinutes(1);
            result.Seconds.ShouldBe(60);
        }
        
        [Fact]
        public void GetUtcNow_Test()
        { 
            var result= TimestampHelper.GetUtcNow();
            result.ShouldNotBeNull();
        }
    }
}