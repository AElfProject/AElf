using System;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.CSharp.Core.Utils
{
    public class PreconditionsTests
    {
        [Fact]
        public void PreCondition_Check_Test()
        {
            Func<Address,string> func = null;
            Should.Throw<ArgumentException>(() => Preconditions.CheckNotNull(func));
            Should.Throw<ArgumentException>(() => Preconditions.CheckNotNull(func, nameof(func))).Message
                .ShouldContain(nameof(func));
            
            func = address => address.ToBase58();
            Preconditions.CheckNotNull(func).ShouldBe(func);
            Preconditions.CheckNotNull(func, nameof(func)).ShouldBe(func);
        }
    }
}