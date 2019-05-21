using System;
using AElf.CSharp.Core.Utils;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.CSharp.Core
{
    public class PreconditionsTests : TypesCSharpTestBase
    {
        [Fact]
        public void PreCondition_CheckTest()
        {
            Func<Address> func1 = null;
            Should.Throw<ArgumentException>(() => Preconditions.CheckNotNull(func1));
            
            func1 = () => Address.Generate();
            var reference = Preconditions.CheckNotNull(func1);
            reference.ShouldNotBeNull();
            var addressInfo = reference();
            addressInfo.ShouldNotBeNull();
            addressInfo.GetType().ToString().ShouldBe("AElf.Types.Address");

            Func<Address, string> func2 = address => address.GetFormatted();
            var reference1 = Preconditions.CheckNotNull(func2, "address");
            
            reference1.ShouldNotBeNull();
            var result = reference1(Address.Generate());
            result.ShouldNotBeNullOrEmpty();
        }
    }
}