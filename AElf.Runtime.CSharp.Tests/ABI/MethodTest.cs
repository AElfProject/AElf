using System.Collections.Generic;
using AElf.Common;
using AElf.Kernel.ABI;
using AElf.Runtime.CSharp.Core.ABI;
using AElf.Types.CSharp;
using Google.Protobuf;
using Newtonsoft.Json;
using Xunit;

namespace AElf.Runtime.CSharp.Tests.ABI
{
    public class MethodTest
    {
        [Fact]
        public void  Test()
        {
            var methodAbi =
                @"{""ReturnType"": ""bool"", ""IsAsync"": true, ""Params"": [{""Type"": ""bool"", ""Name"": ""Bool""}, {""Type"": ""int"", ""Name"": ""Int""}, {""Type"": ""uint"", ""Name"": ""UInt""}, {""Type"": ""long"", ""Name"": ""Long""}, {""Type"": ""ulong"", ""Name"": ""ULong""}, {""Type"": ""string"", ""Name"": ""String""}, {""Type"": ""byte[]"", ""Name"": ""Bytes""}, {""Type"": ""AElf.Common.Address"", ""Name"": ""Address""}, {""Type"": ""AElf.Common.Hash"", ""Name"": ""Hash""}], ""Name"": ""TestMethod""}";
            var method = JsonParser.Default.Parse<Method>(methodAbi);
            var userInput =
                $@"[true, -32, 32, -64, 64, ""AElf"", ""0x010203"", ""{Address.Zero.GetFormatted()}"", ""{Hash.Zero.ToHex()}""]";
            var serializedParams = method.SerializeParams(JsonConvert.DeserializeObject<List<string>>(userInput));
            var expected = ParamsPacker.Pack(
                (bool) true,
                (int) -32,
                (uint) 32,
                (long) -64,
                (ulong) 64,
                "AElf",
                new byte[] {0x01, 0x02, 0x03},
                Address.Zero,
                Hash.Zero
            );
            Assert.Equal(expected, serializedParams);
        }
    }
}