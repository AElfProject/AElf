using System.Collections.Generic;
using AElf.ABI.CSharp;
using Google.Protobuf;
using Newtonsoft.Json;
using AElf.Kernel;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Xunit;

namespace AElf.ABI.Tests
{
    public class MethodTest
    {
        [Fact]
        public void  Test()
        {
            var methodAbi =
                @"{""ReturnType"": ""bool"", ""IsAsync"": true, ""Params"": [{""Type"": ""bool"", ""Name"": ""Bool""}, {""Type"": ""int"", ""Name"": ""Int""}, {""Type"": ""uint"", ""Name"": ""UInt""}, {""Type"": ""long"", ""Name"": ""Long""}, {""Type"": ""ulong"", ""Name"": ""ULong""}, {""Type"": ""string"", ""Name"": ""String""}, {""Type"": ""byte[]"", ""Name"": ""Bytes""}, {""Type"": ""AElf.Kernel.Hash"", ""Name"": ""Hash""}], ""Name"": ""TestMethod""}";
            var method = JsonParser.Default.Parse<Method>(methodAbi);
            var userInput =
                @"[true, -32, 32, -64, 64, ""AElf"", ""0x010203"", ""0x55303af821a1d194227a40b81ebad0cb2078b6d662828df19efbd96a310571fe""]";
            var serializedParams = method.SerializeParams(JsonConvert.DeserializeObject<List<string>>(userInput));
            var expected = ParamsPacker.Pack(
                (bool) true,
                (int) -32,
                (uint) 32,
                (long) -64,
                (ulong) 64,
                "AElf",
                new byte[] {0x01, 0x02, 0x03},
                new Kernel.Hash()
                {
                    Value = ByteString.CopyFrom(new byte[]
                    {
                        0x55, 0x30, 0x3a, 0xf8, 0x21, 0xa1, 0xd1, 0x94, 0x22, 0x7a, 0x40, 0xb8, 0x1e, 0xba, 0xd0, 0xcb,
                        0x20, 0x78, 0xb6, 0xd6, 0x62, 0x82, 0x8d, 0xf1, 0x9e, 0xfb, 0xd9, 0x6a, 0x31, 0x05, 0x71, 0xfe
                    })
                }
            );
            Assert.Equal(expected, serializedParams);
        }
    }
}