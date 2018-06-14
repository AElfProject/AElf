using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.SmartContracts.CSharpSmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ServiceStack;
using Xunit;
using AElf.Runtime.CSharp;
using Xunit.Frameworks.Autofac;
using AElf.Contracts;
using Path = AElf.Kernel.Path;
using AElf.Types.CSharp;

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

            Assert.Equal((ulong)190, bal10);
            Assert.Equal((ulong)180, bal20);

            Assert.Equal((ulong)110, bal11);
            Assert.Equal((ulong)120, bal21);

        }
    }
}
