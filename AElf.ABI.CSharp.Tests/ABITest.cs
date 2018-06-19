using System.IO;
using ServiceStack;
using Google.Protobuf;
using AElf.ABI.CSharp;
using Xunit;

namespace AElf.ABI.Tests
{
    public class ABITest
    {
        [Fact]
        public void Test()
        {
            string filePath = "../../../../AElf.ABI.CSharp.Tests.TestContract/bin/Debug/netstandard2.0/AElf.ABI.CSharp.Tests.TestContract.dll";
            string expected = @"{ ""Name"": ""AElf.ABI.CSharp.Tests.UserContract"", ""Methods"": [ { ""Name"": ""InvokeAsync"", ""Params"": [ ], ""ReturnType"": ""void"", ""IsAsync"": true }, { ""Name"": ""GetTotalSupply"", ""Params"": [ ], ""ReturnType"": ""uint"", ""IsAsync"": false }, { ""Name"": ""GetBalanceOf"", ""Params"": [ { ""Type"": ""AElf.Kernel.Hash"", ""Name"": ""account"" } ], ""ReturnType"": ""uint"", ""IsAsync"": false }, { ""Name"": ""Transfer"", ""Params"": [ { ""Type"": ""AElf.Kernel.Hash"", ""Name"": ""to"" }, { ""Type"": ""uint"", ""Name"": ""value"" } ], ""ReturnType"": ""bool"", ""IsAsync"": false }, { ""Name"": ""SetAccount"", ""Params"": [ { ""Type"": ""string"", ""Name"": ""name"" }, { ""Type"": ""AElf.Kernel.Hash"", ""Name"": ""address"" } ], ""ReturnType"": ""bool"", ""IsAsync"": true }, { ""Name"": ""GetAccountName"", ""Params"": [ ], ""ReturnType"": ""string"", ""IsAsync"": true } ], ""Events"": [ { ""Name"": ""AElf.ABI.CSharp.Tests.Transfered"", ""Indexed"": [ ], ""NonIndexed"": [ { ""Type"": ""AElf.Kernel.Hash"", ""Name"": ""From"" }, { ""Type"": ""AElf.Kernel.Hash"", ""Name"": ""To"" }, { ""Type"": ""uint"", ""Name"": ""Value"" } ] }, { ""Name"": ""AElf.ABI.CSharp.Tests.AccountName"", ""Indexed"": [ { ""Type"": ""string"", ""Name"": ""Name"" } ], ""NonIndexed"": [ { ""Type"": ""string"", ""Name"": ""Dummy"" } ] } ], ""Types"": [ { ""Name"": ""AElf.ABI.CSharp.Tests.Account"", ""Fields"": [ { ""Type"": ""string"", ""Name"": ""Name"" }, { ""Type"": ""AElf.Kernel.Hash"", ""Name"": ""Address"" } ] } ] }";
            Module module = Generator.GetABIModule(GetCode(filePath));
            string actual = new JsonFormatter(new JsonFormatter.Settings(true)).Format(module);
            /*
             * Generate an abi file
            using (var stream = File.Open("../../../AElf.ABI.CSharp.Tests.TestContract.abi", FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                stream.Write(actual);
            }
            */
            Assert.Equal(expected, actual);
        }

        public static byte[] GetCode(string filePath)
        {
            byte[] code;
            using (var file = File.OpenRead(Path.GetFullPath(filePath)))
            {
                code = file.ReadFully();
            }
            return code;
        }
    }
}
