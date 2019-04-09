using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Kernel.ABI;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Runtime.CSharp.Tests.TestContract;
using AElf.Types.CSharp;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace AElf.Runtime.CSharp.Tests
{
    public sealed class SmartContractRunnerTest: CSharpRuntimeTestBase
    {
        private ISmartContractRunner Runner { get; set; }
        private SmartContractRegistration Reg { get; set; }

        public SmartContractRunnerTest()
        {
            var contractCode = File.ReadAllBytes(typeof(TestContract.TestContract).Assembly.Location);
            Runner = GetRequiredService<ISmartContractRunner>();
            Reg = new SmartContractRegistration()
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = ByteString.CopyFrom(contractCode),
                CodeHash = Hash.FromRawBytes(contractCode)
            };
        }

        [Fact]
        public async Task Get_IExecutive()
        {
            var executive = await Runner.RunAsync(Reg);
            executive.ShouldNotBe(null);
        }

        [Fact]
        public void Contract_ExtraMetadata()
        {
            var contractType = typeof(TestContract.TestContract);
            var contractMetadataTemplate = Runner.ExtractMetadata(contractType);
            contractMetadataTemplate.FullName.ShouldBe("AElf.Runtime.CSharp.Tests.TestContract.TestContract");
            contractMetadataTemplate.ProcessFunctionOrder.Count.ShouldBeGreaterThanOrEqualTo(12);
            contractMetadataTemplate.ProcessFunctionOrder.Contains("${this}.TestBoolState").ShouldBeTrue();
        }
    }
}