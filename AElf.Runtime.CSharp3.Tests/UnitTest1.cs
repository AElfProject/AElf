using System;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Runtime.CSharp3.Tests.TestContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace AElf.Runtime.CSharp3.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var types = typeof(ContractApi).Assembly.GetTypes();
//            Console.WriteLine(string.Join("\r\n",
//                types.Select(x => $"{x.FullName} {typeof(ISmartContract).IsAssignableFrom(x)} {x.IsNested}")));
            Console.WriteLine(new StringValue(){Value = "nishishei"}.ToByteArray().ToHex());
        }
    }
}