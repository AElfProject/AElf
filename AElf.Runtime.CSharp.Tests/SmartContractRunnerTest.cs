using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Kernel.ABI;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types.CSharp;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace AElf.Runtime.CSharp.Tests
{
    //TODO: should not use Token contract to do test. should create a new contract to test. 
    //BODY: token may be changed in the future, but the tests do not need to be changed.
    /*public sealed class SmartContractRunnerTest: CSharpRuntimeTestBase
    {
        private ISmartContractRunner Runner { get; set; }
        private SmartContractRegistration Reg { get; set; }

        public SmartContractRunnerTest()
        {
            var contractCode = File.ReadAllBytes(typeof(TokenContract).Assembly.Location);
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

        [Fact]
        public async Task Get_JsonStringParameter()
        {
            var executive = await Runner.RunAsync(Reg);
            //TransferFrom parameter
            var addressFrom = Address.Generate();
            var addressTo = Address.Generate();
            var byteString = ByteString.CopyFrom(ParamsPacker.Pack(addressFrom, addressTo, 1000UL));
            var parameterObj = executive.GetJsonStringOfParameters(nameof(TokenContract.TransferFrom), byteString.ToByteArray());
            
            parameterObj.ShouldNotBeNull();
            var jsonParameter = (JObject) JsonConvert.DeserializeObject(parameterObj);
            jsonParameter.Count.ShouldBe(3);
            jsonParameter["from"].ShouldBe(addressFrom.GetFormatted());
            jsonParameter["to"].ShouldBe(addressTo.GetFormatted());
            jsonParameter["amount"].To<ulong>().ShouldBe(1000UL);
        }

        [Fact]
        public async Task Get_ReturnValue()
        {
            var executive = await Runner.RunAsync(Reg);
            var resultArray = "ELF".GetBytes();
            var returnObj = executive.GetReturnValue(nameof(TokenContract.TokenName), resultArray);
            returnObj.ToString().ShouldBe("ELF");
        }
    }*/
}