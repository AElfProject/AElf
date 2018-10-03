using System.Collections.Generic;
using AElf.Configuration.Config.Contract;
using AElf.Kernel;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Common;
using Google.Protobuf;

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
            Address account0 = Address.FromBytes(Hash.Generate().ToByteArray());
            Address account1 = Address.FromBytes(Hash.Generate().ToByteArray());

            // Initialize
            _contract1.Initialize(account0, 200);
            _contract1.Initialize(account1, 100);
            _contract2.Initialize(account0, 200);
            _contract2.Initialize(account1, 100);

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
            var bl1 = new List<string>
            {
                @"System\.Reflection\..*",
                @"System\.IO\..*",
                @"System\.Net\..*",
                @"System\.Threading\..*",
            };

            RunnerConfig.Instance.SdkDir = _mock.SdkDir;
            RunnerConfig.Instance.BlackList = bl1;
            RunnerConfig.Instance.WhiteList = new List<string>();
            
            var runner1 = new SmartContractRunner();
            runner1.CodeCheck(_mock.ContractCode, true);

            var bl2 = new List<string>
            {
                @".*"
            };
            
            RunnerConfig.Instance.BlackList = bl2;
            
            var runner2 = new SmartContractRunner();
            Assert.Throws<InvalidCodeException>(()=>runner2.CodeCheck(_mock.ContractCode, true));
        }
    }
}