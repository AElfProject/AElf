using AElf.Kernel;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Runtime.CSharp.Tests
{
    [UseAutofacTestFramework]
    public class RunnerTest
    {
        private MockSetup _mock;
        private TestContractShim _contract1;
        private TestContractShim _contract2;

        public RunnerTest(MockSetup mock)
        {
            _mock = mock;
            _contract1 = new TestContractShim(_mock);
            _contract2 = new TestContractShim(_mock, true);
        }

        [Fact]
        public void Test()
        {
            Hash account0 = Hash.Generate();
            Hash account1 = Hash.Generate();

            // Initialize
            _contract1.InitializeAsync(account0, 200);
            _contract1.InitializeAsync(account1, 100);
            _contract2.InitializeAsync(account0, 200);
            _contract2.InitializeAsync(account1, 100);

            // Transfer
            _contract1.Transfer(account0, account1, 10);
            _contract2.Transfer(account0, account1, 20);

            // Check balance
            var bal10 = _contract1.GetBalance(account0);
            var bal20 = _contract2.GetBalance(account0);
            var bal11 = _contract1.GetBalance(account1);
            var bal21 = _contract2.GetBalance(account1);

            Assert.Equal((ulong) 190, bal10);
            Assert.Equal((ulong) 180, bal20);

            Assert.Equal((ulong) 110, bal11);
            Assert.Equal((ulong) 120, bal21);
        }

        [Fact]
        public void CodeCheckTest()
        {
            var bl1 = new []
            {
                @"System\.Reflection\..*",
                @"System\.IO\..*",
                @"System\.Net\..*",
                @"System\.Threading\..*",
            };
            var runner1 = new SmartContractRunner(new RunnerConfig()
            {
                SdkDir = _mock.SdkDir,
                BlackList = bl1,
                WhiteList = new string[] {}
            });
            runner1.CodeCheck(_mock.ContractCode, true);

            var bl2 = new []
            {
                @".*"
            };
            
            var runner2 = new SmartContractRunner(new RunnerConfig()
            {
                SdkDir = _mock.SdkDir,
                BlackList = bl2,
                WhiteList = new string[] {}
            });
            Assert.Throws<InvalidCodeException>(()=>runner2.CodeCheck(_mock.ContractCode, true));
        }
    }
}