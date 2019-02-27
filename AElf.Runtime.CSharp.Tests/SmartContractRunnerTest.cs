using System.IO;
using System.Linq;
using AElf.Common;
using AElf.Contracts.Token;
using AElf.Kernel;
using AElf.Kernel.ABI;
using AElf.Kernel.SmartContract.Infrastructure;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Runtime.CSharp.Tests
{
    public class SmartContractRunnerTest: CSharpRuntimeTestBase
    {
        private ISmartContractRunner Runner { get; set; }

        public SmartContractRunnerTest()
        {
            Runner = GetRequiredService<ISmartContractRunner>();
        }

        [Fact]
        public void Get_ContractContext()
        {
            var contractCode = File.ReadAllBytes(typeof(TokenContract).Assembly.Location);
            var reg = new SmartContractRegistration()
            {
                Category = 2,
                Code = ByteString.CopyFrom(contractCode),
                CodeHash = Hash.FromRawBytes(contractCode)

            };
            var executive = Runner.RunAsync(reg);
            executive.ShouldNotBe(null);
        }

        [Fact]
        public void Get_AbiModule()
        {
            var contractCode = File.ReadAllBytes(typeof(TokenContract).Assembly.Location);
            var reg = new SmartContractRegistration()
            {
                Category = 2,
                Code = ByteString.CopyFrom(contractCode),
                CodeHash = Hash.FromRawBytes(contractCode)

            };
            var message = Runner.GetAbi(reg) as Module;
            message.ShouldNotBe(null);
            var eventList = message.Events.Select(o => o.Name).ToList();
            eventList.Contains("AElf.Contracts.Token.Transferred").ShouldBe(true);
            eventList.Contains("AElf.Contracts.Token.Approved").ShouldBe(true);
            eventList.Contains("AElf.Contracts.Token.UnApproved").ShouldBe(true);
            eventList.Contains("AElf.Contracts.Token.Burned").ShouldBe(true);

            var methodList = message.Methods.Select(o=>o.Name).ToList();
            methodList.Contains("Initialize").ShouldBe(true);
            methodList.Contains("Transfer").ShouldBe(true);
            methodList.Contains("TransferFrom").ShouldBe(true);
            methodList.Contains("Approve").ShouldBe(true);
            methodList.Contains("UnApprove").ShouldBe(true);
            methodList.Contains("Burn").ShouldBe(true);
            methodList.Contains("Symbol").ShouldBe(true);
            methodList.Contains("TokenName").ShouldBe(true);
            methodList.Contains("TotalSupply").ShouldBe(true);
        }
    }
}