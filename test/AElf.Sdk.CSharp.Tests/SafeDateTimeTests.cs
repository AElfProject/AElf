using System;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using Shouldly;

namespace AElf.Sdk.CSharp.Tests
{
    public class SafeDateTimeTests
    {
        private SafeDateTime now_safe;
        private DateTime now_dt;

        public SafeDateTimeTests()
        {
            var now = DateTime.Now;
            now_safe = new SafeDateTime(now);
            now_dt = now.ToUniversalTime();
        }

        [Fact]
        public void Universal_Time_Test1()
        {
            now_safe.ToDateTime().Kind.ShouldBe(DateTimeKind.Utc);
        }
        
        [Fact]
        public void Universal_Time_Test2()
        {
            now_dt.ToSafeDateTime().ToDateTime().Kind.ShouldBe(DateTimeKind.Utc);
        }

        [Fact]
        public void Add_Days_Test()
        {
            now_safe.AddDays(10).ToDateTime().ShouldBe(now_dt.AddDays(10));
        }
        
        [Fact]
        public void Add_Hours_Test()
        {
            now_safe.AddHours(10).ToDateTime().ShouldBe(now_dt.AddHours(10));
        }
        
        [Fact]
        public void Add_Minutes_Test()
        {
            now_safe.AddMinutes(10).ToDateTime().ShouldBe(now_dt.AddMinutes(10));
        }
        
        [Fact]
        public void Add_Seconds_Test()
        {
            now_safe.AddSeconds(10).ToDateTime().ShouldBe(now_dt.AddSeconds(10));
        }
        
        [Fact]
        public void Add_Milliseconds_Test()
        {
            now_safe.AddMilliseconds(10).ToDateTime().ShouldBe(now_dt.AddMilliseconds(10));
        }
    }
}
