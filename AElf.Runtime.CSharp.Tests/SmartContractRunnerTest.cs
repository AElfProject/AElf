using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Kernel.ABI;
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

            executive.SetMaxCallDepth(3);
        }

        [Fact]
        public void Get_AbiModule()
        {
            var message = Runner.GetAbi(Reg) as Module;
            message.ShouldNotBe(null);

            var methodList = message.Methods.Select(o=>o.Name).ToList();
            methodList.Contains("TestBoolState").ShouldBeTrue();
            methodList.Contains("TestInt32State").ShouldBeTrue();
            methodList.Contains("TestUInt32State").ShouldBeTrue();
        }
        
        //TODO: GetJsonStringOfParameters cannot deserialize Protobuf to string message correctly. Please refer below two test cases result.
        [Fact(Skip = "Not passed due to convert json string with some special string value.")]
        public async Task Get_JsonString_From_StringInput()
        {
            var executive = await Runner.RunAsync(Reg);
            //TestStringState parameter
            var byteString = ByteString.CopyFrom(ParamsPacker.Pack(new StringInput
            {
                StringValue = "test string parameter"
            }));
            var parameterObj = executive.GetJsonStringOfParameters(nameof(TestContract.TestContract.TestStringState), byteString.ToByteArray());
            parameterObj.ShouldNotBeNull();
            var jsonParameter = (JObject) JsonConvert.DeserializeObject(parameterObj);
            jsonParameter.Count.ShouldBe(1);
            jsonParameter["StringValue"].ToString().ShouldBe("test string parameter");
        }

        [Fact(Skip = "Not passed due to convert json from bool value return empty.")]
        public async Task Get_JsonString_From_BoolInput()
        {
            var executive = await Runner.RunAsync(Reg);
            //TestBoolState parameter
            var byteString = ByteString.CopyFrom(ParamsPacker.Pack(new BoolInput()
            {
                BoolValue = true
            }));
            var parameterObj = executive.GetJsonStringOfParameters(nameof(TestContract.TestContract.TestBoolState), byteString.ToByteArray());
            parameterObj.ShouldNotBeNull();
            var jsonParameter = (JObject) JsonConvert.DeserializeObject(parameterObj);
            jsonParameter.Count.ShouldBe(1);
            jsonParameter["BoolValue"].ToString().ShouldBe("test string parameter");
        }
    }
}