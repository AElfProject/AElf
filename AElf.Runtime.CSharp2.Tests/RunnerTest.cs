using System.Collections.Generic;
using AElf.Kernel;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Common;

namespace AElf.Runtime.CSharp2.Tests
{
    public class RunnerTest : CSharpRuntimeTestBase
    {
        private MockSetup _mock;
        private readonly TestContractShim _contract1;

        public RunnerTest()
        {
            _mock = GetRequiredService<MockSetup>();
            _contract1 = new TestContractShim(_mock);
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