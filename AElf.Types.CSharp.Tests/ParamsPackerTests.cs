using System;
using Xunit;
using Google.Protobuf;

namespace AElf.Types.CSharp.Tests
{
    public class ParamsPackerTests
    {
        [Fact]
        public void Test()
        {
            var objs = new object[]{
                true,
                (int)-32,
                (uint)32,
                (long)-64,
                (ulong)64,
                "AElf",
                new byte[]{0x1, 0x2, 0x3}
            };
            var serialized = new TestParams()
            {
                Field1 = (bool)objs[0],
                Field2 = (int)objs[1],
                Field3 = (uint)objs[2],
                Field4 = (long)objs[3],
                Field5 = (ulong)objs[4],
                Field6 = (string)objs[5],
                Field7 = ByteString.CopyFrom((byte[])objs[6])
            }.ToByteArray();
            Assert.Equal(serialized, ParamsPacker.Pack(
                objs
            ));
        }
    }
}
