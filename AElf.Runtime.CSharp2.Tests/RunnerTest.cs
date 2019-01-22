using System.Collections.Generic;
using AElf.Kernel;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Common;
using Google.Protobuf;

namespace AElf.Runtime.CSharp2.Tests
{
    [UseAutofacTestFramework]
    public class RunnerTest
    {
        private readonly TestContractShim _contract1;

        public RunnerTest(MockSetup mock)
        {
            _contract1 = new TestContractShim(mock);
        }

        [Fact]
        public void Test()
        {
            Address account0 = Address.Generate();

            _contract1.Initialize("ELF", "ELF Token", 1000000000, 0);
            Assert.Equal(1000000000UL, _contract1.BalanceOf(Address.Zero));
            _contract1.Transfer(Address.Zero, account0, 90);
            Assert.Equal(1000000000UL - 90UL, _contract1.BalanceOf(Address.Zero));
            Assert.Equal(90UL, _contract1.BalanceOf(account0));
        }
    }
}